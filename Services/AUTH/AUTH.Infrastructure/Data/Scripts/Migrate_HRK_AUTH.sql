/* Run on the SAME SQL Server instance as HRK, with AUTH/GAME writers stopped.
   Back up HRK first. Creates HRK_AUTH; copies and verifies before dropping source tables.
   Deliberately refuses existing destination tables and unsupported schema features.
   GAME/ACTIVITY outboxes are NOT AUTH data and are never moved.
   Requires CREATE DATABASE and ALTER permissions in both databases. SQL Server 2017+.
*/
USE master;
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_ID(N'HRK') IS NULL THROW 51000, 'Source database HRK does not exist.', 1;
IF DB_ID(N'HRK_AUTH') IS NULL
BEGIN
    DECLARE @create nvarchar(max) = N'CREATE DATABASE HRK_AUTH COLLATE ' +
        CONVERT(sysname, DATABASEPROPERTYEX(N'HRK', 'Collation'));
    EXEC (@create);
END;
GO
USE HRK;
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET LOCK_TIMEOUT 30000;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource=N'HRK_AUTH.database-migration.v1',
        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000;
    IF @lock < 0 THROW 51000, 'Another AUTH migration is running.', 1;

    CREATE TABLE #Move (Name sysname COLLATE DATABASE_DEFAULT PRIMARY KEY, ObjectId int NULL);
    INSERT #Move(Name) VALUES
        (N'HRK_Users'), (N'HRK_Roles'), (N'HRK_UserRoles'), (N'HRK_UserClaims'),
        (N'HRK_UserLogins'), (N'HRK_RoleClaims'), (N'HRK_UserTokens'),
        (N'HRK_LoginSessions'), (N'HRK_RefreshTokens'), (N'HRK_ApplicationRouteConfigs');
    -- Only AUTH-specific messaging tables may follow an earlier partial deployment.
    INSERT #Move(Name) SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo')
        AND name IN(N'HRK_AuthOutboxMessages',N'HRK_AuthProcessedMessages');
    UPDATE #Move SET ObjectId = OBJECT_ID(N'dbo.' + QUOTENAME(Name), N'U');
    IF NOT EXISTS (SELECT 1 FROM #Move WHERE ObjectId IS NOT NULL)
    BEGIN
        IF EXISTS (SELECT 1 FROM #Move m WHERE NOT EXISTS
            (SELECT 1 FROM HRK_AUTH.sys.tables t JOIN HRK_AUTH.sys.schemas s ON s.schema_id=t.schema_id
             WHERE t.name=m.Name AND s.name=N'dbo'))
            THROW 51000, 'Source absent but target incomplete. Restore backup or inspect manually.', 1;
        COMMIT;
        PRINT 'AUTH tables were already migrated. No changes made.';
        RETURN;
    END;
    IF EXISTS (SELECT 1 FROM #Move WHERE ObjectId IS NULL)
        THROW 51000, 'One of the ten expected AUTH tables is missing. Inspect schema before migration.', 1;
    IF EXISTS (SELECT 1 FROM #Move m JOIN HRK_AUTH.sys.tables t ON t.name=m.Name
        JOIN HRK_AUTH.sys.schemas s ON s.schema_id=t.schema_id AND s.name=N'dbo')
        THROW 51000, 'Target already contains AUTH tables. Refusing to overwrite data.', 1;

    -- Fail closed rather than silently lose special column/table semantics.
    IF EXISTS (SELECT 1 FROM sys.tables t JOIN #Move m ON m.ObjectId=t.object_id
        WHERE t.temporal_type<>0 OR t.is_memory_optimized=1 OR t.is_replicated=1 OR t.is_tracked_by_cdc=1)
        OR EXISTS (SELECT 1 FROM sys.columns c JOIN #Move m ON m.ObjectId=c.object_id
        WHERE c.is_sparse=1 OR c.is_filestream=1 OR c.is_rowguidcol=1 OR c.encryption_type IS NOT NULL
           OR c.system_type_id IN (34,35,99,189,240,241) OR c.user_type_id<>c.system_type_id)
        OR EXISTS (SELECT 1 FROM sys.computed_columns c JOIN #Move m ON m.ObjectId=c.object_id WHERE c.definition IS NULL)
        OR EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d
            JOIN sys.computed_columns c ON c.object_id=d.referencing_id AND c.column_id=d.referencing_minor_id
            JOIN #Move m ON m.ObjectId=c.object_id
            WHERE (d.referenced_id IS NOT NULL AND d.referenced_id<>m.ObjectId)
               OR d.referenced_database_name IS NOT NULL OR d.referenced_server_name IS NOT NULL)
        OR EXISTS (SELECT 1 FROM sys.triggers t JOIN #Move m ON m.ObjectId=t.parent_id)
        OR EXISTS (SELECT 1 FROM sys.indexes i JOIN #Move m ON m.ObjectId=i.object_id
                   WHERE i.type NOT IN (0,1,2) OR i.is_disabled=1 OR i.is_hypothetical=1)
        THROW 51000, 'Unsupported AUTH schema feature; migration stopped without deleting data.', 1;

    IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies d JOIN #Move m ON m.ObjectId=d.referenced_id
        JOIN sys.objects o ON o.object_id=d.referencing_id WHERE o.type IN ('V','P','FN','IF','TF'))
        THROW 51000, 'A SQL view/function/procedure depends on AUTH tables. Update it before migrating.', 1;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys f JOIN #Move m ON m.ObjectId=f.parent_object_id
        WHERE NOT EXISTS (SELECT 1 FROM #Move p WHERE p.ObjectId=f.referenced_object_id))
        THROW 51000, 'AUTH references a table outside the migration set.', 1;
    -- Only the known GAME player -> AUTH user relationship may be removed.
    IF EXISTS (SELECT 1 FROM sys.foreign_keys f JOIN #Move m ON m.ObjectId=f.referenced_object_id
        WHERE NOT EXISTS (SELECT 1 FROM #Move p WHERE p.ObjectId=f.parent_object_id)
        AND NOT (f.parent_object_id=ISNULL(OBJECT_ID(N'dbo.HRK_Players'),-1) AND m.Name=N'HRK_Users'
          AND (SELECT COUNT(*) FROM sys.foreign_key_columns c WHERE c.constraint_object_id=f.object_id)=1
          AND EXISTS (SELECT 1 FROM sys.foreign_key_columns c WHERE c.constraint_object_id=f.object_id
              AND COL_NAME(c.parent_object_id,c.parent_column_id)=N'UserId'
              AND COL_NAME(c.referenced_object_id,c.referenced_column_id)=N'Id')))
        THROW 51000, 'Unexpected external foreign key references AUTH. Review before migration.', 1;

    DECLARE @name sysname, @oid int, @sql nvarchar(max), @columns nvarchar(max), @rows bigint;
    DECLARE tables_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name,ObjectId FROM #Move ORDER BY Name;
    OPEN tables_cursor;
    FETCH NEXT FROM tables_cursor INTO @name,@oid;
    WHILE @@FETCH_STATUS=0
    BEGIN
        -- Locks are held until copy/validation/drop commit. Stop applications first to avoid stale connections.
        SET @sql=N'SELECT @n=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@name)+N' WITH (TABLOCKX,HOLDLOCK);';
        EXEC sys.sp_executesql @sql,N'@n bigint OUTPUT',@n=@rows OUTPUT;
        FETCH NEXT FROM tables_cursor INTO @name,@oid;
    END;
    CLOSE tables_cursor;
    OPEN tables_cursor;
    FETCH NEXT FROM tables_cursor INTO @name,@oid;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @columns=STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(name)),N',') WITHIN GROUP (ORDER BY column_id)
        FROM sys.columns WHERE object_id=@oid AND is_computed=0;
        -- A direct single-table SELECT INTO retains identity, types, nullability and column collations.
        SET @sql=N'SELECT '+@columns+N' INTO HRK_AUTH.dbo.'+QUOTENAME(@name)+N' FROM dbo.'+QUOTENAME(@name)+N';'+
            N'IF (SELECT COUNT_BIG(*) FROM dbo.'+QUOTENAME(@name)+N')<>(SELECT COUNT_BIG(*) FROM HRK_AUTH.dbo.'+QUOTENAME(@name)+N')'+
            N' OR EXISTS(SELECT '+@columns+N' FROM dbo.'+QUOTENAME(@name)+N' EXCEPT SELECT '+@columns+N' FROM HRK_AUTH.dbo.'+QUOTENAME(@name)+N')'+
            N' OR EXISTS(SELECT '+@columns+N' FROM HRK_AUTH.dbo.'+QUOTENAME(@name)+N' EXCEPT SELECT '+@columns+N' FROM dbo.'+QUOTENAME(@name)+N')'+
            N' THROW 51000, ''AUTH copy validation failed.'', 1;';
        EXEC (@sql);
        -- Copy stored data only; recreate computed columns from their original definitions.
        -- Materializing values would lose semantics (e.g. LoginSessions.IsActive after logout).
        DECLARE @computedName sysname, @computedDefinition nvarchar(max), @persisted bit, @nullable bit;
        DECLARE computed_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT name,definition,is_persisted,is_nullable FROM sys.computed_columns
            WHERE object_id=@oid ORDER BY column_id;
        OPEN computed_cursor;
        FETCH NEXT FROM computed_cursor INTO @computedName,@computedDefinition,@persisted,@nullable;
        WHILE @@FETCH_STATUS=0
        BEGIN
            SET @sql=N'USE HRK_AUTH; ALTER TABLE dbo.'+QUOTENAME(@name)+N' ADD '+QUOTENAME(@computedName)+
                N' AS '+@computedDefinition+CASE WHEN @persisted=1 THEN N' PERSISTED' ELSE N'' END+
                CASE WHEN @persisted=1 AND @nullable=0 THEN N' NOT NULL' ELSE N'' END+N';';
            EXEC (@sql);
            FETCH NEXT FROM computed_cursor INTO @computedName,@computedDefinition,@persisted,@nullable;
        END;
        CLOSE computed_cursor;
        DEALLOCATE computed_cursor;
        DECLARE @last nvarchar(100) = (SELECT CONVERT(nvarchar(100),last_value) FROM sys.identity_columns WHERE object_id=@oid);
        IF @last IS NOT NULL
        BEGIN
            SET @sql=N'SELECT @n=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@name)+N';';
            EXEC sys.sp_executesql @sql,N'@n bigint OUTPUT',@n=@rows OUTPUT;
            -- On a newly created empty table, the first insert uses the reseed value itself.
            -- An old table emptied by DELETE must instead continue at last_value + increment.
            IF @rows=0
                SELECT @last=CONVERT(nvarchar(100),CONVERT(decimal(38,0),last_value)+CONVERT(decimal(38,0),increment_value))
                FROM sys.identity_columns WHERE object_id=@oid;
            SET @sql=N'USE HRK_AUTH; DBCC CHECKIDENT (N''dbo.'+REPLACE(@name,'''','''''')+N''', RESEED, '+@last+N') WITH NO_INFOMSGS;';
            EXEC (@sql);
        END;
        FETCH NEXT FROM tables_cursor INTO @name,@oid;
    END;
    CLOSE tables_cursor;
    DEALLOCATE tables_cursor;

    -- Rebuild PKs, unique constraints and ordinary indexes (including filters, INCLUDE and sort direction).
    DECLARE @index int, @indexName sysname, @type nvarchar(20), @pk bit, @unique bit, @constraint bit,
        @keys nvarchar(max), @include nvarchar(max), @filter nvarchar(max);
    DECLARE index_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT m.Name,i.object_id,i.index_id,i.name,i.type_desc,i.is_primary_key,i.is_unique,i.is_unique_constraint,i.filter_definition
        FROM #Move m JOIN sys.indexes i ON i.object_id=m.ObjectId WHERE i.index_id>0
        ORDER BY m.Name,i.index_id;
    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @name,@oid,@index,@indexName,@type,@pk,@unique,@constraint,@filter;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @keys=STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)+CASE WHEN ic.is_descending_key=1 THEN N' DESC' ELSE N' ASC' END),N',')
            WITHIN GROUP (ORDER BY ic.key_ordinal)
        FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE ic.object_id=@oid AND ic.index_id=@index AND ic.key_ordinal>0;
        SELECT @include=STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(c.name)),N',') WITHIN GROUP (ORDER BY ic.index_column_id)
        FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE ic.object_id=@oid AND ic.index_id=@index AND ic.is_included_column=1;
        SET @sql=N'USE HRK_AUTH; '+ CASE WHEN @pk=1 OR @constraint=1 THEN
            N'ALTER TABLE dbo.'+QUOTENAME(@name)+N' ADD CONSTRAINT '+QUOTENAME(@indexName)+
            CASE WHEN @pk=1 THEN N' PRIMARY KEY ' ELSE N' UNIQUE ' END+@type+N' ('+@keys+N');'
        ELSE N'CREATE '+CASE WHEN @unique=1 THEN N'UNIQUE ' ELSE N'' END+@type+N' INDEX '+QUOTENAME(@indexName)+
            N' ON dbo.'+QUOTENAME(@name)+N' ('+@keys+N')'+CASE WHEN @include IS NULL THEN N'' ELSE N' INCLUDE ('+@include+N')' END+
            CASE WHEN @filter IS NULL THEN N'' ELSE N' WHERE '+@filter END+N';' END;
        EXEC (@sql);
        FETCH NEXT FROM index_cursor INTO @name,@oid,@index,@indexName,@type,@pk,@unique,@constraint,@filter;
    END;
    CLOSE index_cursor;
    DEALLOCATE index_cursor;

    DECLARE @ddl nvarchar(max);
    DECLARE constraints_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT N'USE HRK_AUTH; ALTER TABLE dbo.'+(QUOTENAME(m.Name) COLLATE DATABASE_DEFAULT)+N' ADD CONSTRAINT '+QUOTENAME(d.name)+
            N' DEFAULT '+d.definition+N' FOR '+QUOTENAME(c.name)+N';'
        FROM #Move m JOIN sys.default_constraints d ON d.parent_object_id=m.ObjectId
        JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
        UNION ALL
        SELECT N'USE HRK_AUTH; ALTER TABLE dbo.'+(QUOTENAME(m.Name) COLLATE DATABASE_DEFAULT)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(c.name)+
            N' CHECK '+c.definition+N';'
        FROM #Move m JOIN sys.check_constraints c ON c.parent_object_id=m.ObjectId;
    OPEN constraints_cursor;
    FETCH NEXT FROM constraints_cursor INTO @ddl;
    WHILE @@FETCH_STATUS=0 BEGIN EXEC (@ddl); FETCH NEXT FROM constraints_cursor INTO @ddl; END;
    CLOSE constraints_cursor;
    DEALLOCATE constraints_cursor;

    DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT N'USE HRK_AUTH; ALTER TABLE dbo.'+(QUOTENAME(m.Name) COLLATE DATABASE_DEFAULT)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(f.name)+N' FOREIGN KEY ('+
            STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(COL_NAME(c.parent_object_id,c.parent_column_id))),N',') WITHIN GROUP(ORDER BY c.constraint_column_id)+
            N') REFERENCES dbo.'+QUOTENAME(OBJECT_NAME(f.referenced_object_id))+N' ('+
            STRING_AGG(CONVERT(nvarchar(max),QUOTENAME(COL_NAME(c.referenced_object_id,c.referenced_column_id))),N',') WITHIN GROUP(ORDER BY c.constraint_column_id)+
            N') ON DELETE '+REPLACE(f.delete_referential_action_desc,N'_',N' ')+
            N' ON UPDATE '+REPLACE(f.update_referential_action_desc,N'_',N' ')+N';'
        FROM sys.foreign_keys f JOIN #Move m ON m.ObjectId=f.parent_object_id
        JOIN sys.foreign_key_columns c ON c.constraint_object_id=f.object_id
        GROUP BY m.Name,f.name,f.referenced_object_id,f.delete_referential_action_desc,f.update_referential_action_desc;
    OPEN fk_cursor;
    FETCH NEXT FROM fk_cursor INTO @ddl;
    WHILE @@FETCH_STATUS=0 BEGIN EXEC (@ddl); FETCH NEXT FROM fk_cursor INTO @ddl; END;
    CLOSE fk_cursor;
    DEALLOCATE fk_cursor;

    -- Only after all destination data, indexes and constraints have succeeded do we remove old tables.
    DECLARE drop_fk_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT N'ALTER TABLE '+QUOTENAME(OBJECT_SCHEMA_NAME(f.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(f.parent_object_id))+
            N' DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
        FROM sys.foreign_keys f WHERE EXISTS(SELECT 1 FROM #Move m WHERE m.ObjectId=f.referenced_object_id OR m.ObjectId=f.parent_object_id);
    OPEN drop_fk_cursor;
    FETCH NEXT FROM drop_fk_cursor INTO @ddl;
    WHILE @@FETCH_STATUS=0 BEGIN EXEC (@ddl); FETCH NEXT FROM drop_fk_cursor INTO @ddl; END;
    CLOSE drop_fk_cursor;
    DEALLOCATE drop_fk_cursor;
    SELECT @sql=STRING_AGG(CONVERT(nvarchar(max),N'DROP TABLE dbo.'+QUOTENAME(Name)+N';'),CHAR(10)) FROM #Move;
    EXEC (@sql);
    COMMIT;
    PRINT 'AUTH tables and data moved to HRK_AUTH. Old AUTH tables removed from HRK.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

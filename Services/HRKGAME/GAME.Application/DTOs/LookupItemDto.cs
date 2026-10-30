using System;
using System.Collections.Generic;

namespace GAME.Application.DTOs
{

    public class LookupItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool? IsDebuff { get; set; }
        public int? DisplayOrder { get; set; }
    }
}

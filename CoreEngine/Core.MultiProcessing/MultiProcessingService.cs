//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace LV.HCS.MultiProcess
//{
//    public class GetPercentProcessResponse : BaseResponse
//    {
//        public bool IsDone { get; set; }
//        public int Percent { get; set; }
//        public MultiProcessStatus Status { get; set; }
//    }
//    /// <summary>
//    /// Danh sách trạng thái của process
//    /// </summary>
//    public enum MultiProcessStatus
//    {
//        Executing, Error, Completed, Cancelled
//    }
//    /// <summary>
//    /// Thông tin của process
//    /// </summary>
//    public class ProcessInfo
//    {
//        public ProcessInfo()
//        {
//            Percent = 0;
//            Status = MultiProcessStatus.Executing;
//        }
//        /// <summary>
//        /// Tỉ lệ % hoàn thành của process
//        /// </summary>
//        public int Percent { get; set; }
//        /// <summary>
//        /// Trạng thái hiện tại của process
//        /// </summary>
//        public MultiProcessStatus Status { get; set; }
//        public string Error { get; set; }
//    }
//    public class MultiProcess
//    {
//        //    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
//        /// <summary>
//        /// Hàm thực hiện chạy process
//        /// </summary>
//        public Action<Hashtable, int, int> DoWork;
//        /// <summary>
//        /// Thông tin process
//        /// </summary>
//        public ProcessInfo pInfo;
//        /// <summary>
//        /// Flag kiểm tra process đã thực hiện xong hay chưa
//        /// </summary>
//        public bool IsDone;
//        /// <summary>
//        /// Dữ liệu cần xử lý của process
//        /// </summary>
//        public Hashtable hashData;
//        /// <summary>
//        /// Vị trí bắt đầu trong bảng dữ liệu cần xử lý
//        /// </summary>
//        public int FromIndex;
//        /// <summary>
//        /// Vị trí kết thúc trong bảng dữ liệu cần xử lý
//        /// </summary>
//        public int ToIndex;
//        /// <summary>
//        /// Khởi tạo process
//        /// </summary>
//        public MultiProcess()
//        {
//            hashData = new Hashtable();
//            pInfo = new ProcessInfo();
//            IsDone = false;
//        }
//        /// <summary>
//        /// Chạy process
//        /// </summary>
//        public void RunAsync()
//        {
//            //Task task = Task.Run(() =>
//            //{
//            //DoWork.Invoke(hashData, FromIndex, ToIndex);
//            DoWork(hashData, FromIndex, ToIndex);
//            //DoWork = null;
//            //pInfo = null;
//            if (hashData != null)
//            {
//                hashData.Clear();
//                hashData = null;
//            }
//            GC.Collect();
//            //});
//        }
//        /// <summary>
//        /// Sự kiện process thực hiện xong
//        /// </summary>
//        /// <param name="status"></param>
//        private void ProcessCompleted(MultiProcessStatus status)
//        {
//            pInfo.Status = status;
//            IsDone = true;
//        }
//        private void ProcessCompleted(MultiProcessStatus status, string Error)
//        {
//            pInfo.Status = status;
//            pInfo.Error = Error;
//            IsDone = true;
//        }
//        /// <summary>
//        /// Tỉ lệ % hoàn thành của process
//        /// </summary>
//        /// <param name="percent"></param>
//        public void ReportPercent(int percent)
//        {
//            pInfo.Percent = percent;

//            if (percent == 100)
//            {
//                ProcessCompleted(MultiProcessStatus.Completed);
//            }
//        }
//        /// <summary>
//        /// Tiến trình bị lỗi
//        /// </summary>
//        public void ReportError(string Error)
//        {
//            ProcessCompleted(MultiProcessStatus.Error, Error);
//            GC.Collect();
//        }

//    }

//    public class MultiProcessCreator
//    {
//        //private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

//        private string sCustomer;
//        private string sFunctionID;
//        private string sUserID;
//        private string sTypeProcess;

//        private static Hashtable hashMultiProcess = new Hashtable();

//        private List<object> DataList;

//        private string ProcessName;

//        private Action<List<object>> DoAction;

//        public int iNumberProcess;

//        public int iItemPerProcess;

//        private bool stopProcess;

//        private string error;

//        public MultiProcessCreator(object request, string processName, List<object> dataList, Action<List<object>> doAction)
//        {
//            DataList = dataList;
//            //sCustomer = (string)request.GetType().GetProperty("Customer").GetValue(request, null);
//            //sFunctionID = (string)request.GetType().GetProperty("FunctionID").GetValue(request, null);
//            //sUserID = (string)request.GetType().GetProperty("UserID").GetValue(request, null);
//            sTypeProcess = null;
//            ProcessName = processName;
//            DoAction = doAction;
//        }
//        /// <summary>
//        /// Tạo multi process
//        /// </summary>
//        /// <param name="request">Class con của BaseRequest</param>
//        /// <param name="processName">Tên process</param>
//        /// <param name="dataList">Dữ liệu cần xử lý</param>
//        /// <param name="doAction">Hàm cần xử lý cho mỗi dòng dữ liệu</param>
//        /// <param name="typeCode">Mã loại của process (trong trường hợp 1 function dùng nhiều hơn 1 thiết lập process)</param>
//        public MultiProcessCreator(object request, string processName, List<object> dataList, Action<List<object>> doAction, string typeCode)
//        {
//            DataList = dataList;
//            //sCustomer = (string)request.GetType().GetProperty("Customer").GetValue(request, null);
//            //sFunctionID = (string)request.GetType().GetProperty("FunctionID").GetValue(request, null);
//            //sUserID = (string)request.GetType().GetProperty("UserID").GetValue(request, null);
//            sTypeProcess = typeCode;
//            ProcessName = processName;
//            DoAction = doAction;
//        }
//        /// <summary>
//        /// Thực hiện chạy các process
//        /// </summary>
//        public void DoWork()
//        {
//            stopProcess = false;
//            error = "";
//            int dataLength = DataList.Count;

//            if (dataLength == 0)
//            {
//                //Tạo biến lưu số lượng process
//                if (hashMultiProcess[ProcessName + "_Count"] == null)
//                {
//                    hashMultiProcess.Add(ProcessName + "_Count", iNumberProcess);
//                }
//                else
//                {
//                    hashMultiProcess[ProcessName + "_Count"] = iNumberProcess;
//                }
//                //Tạo biến chứa danh sách process
//                if (hashMultiProcess[ProcessName] == null)
//                {
//                    hashMultiProcess.Add(ProcessName, new List<MultiProcess>());
//                }
//                else
//                {
//                    hashMultiProcess[ProcessName] = new List<MultiProcess>();
//                }
//            }
//            else
//            {
//                #region Lấy số lượng process

//                iNumberProcess = 1;
//                iItemPerProcess = 1;

//                DB dbProcess = new DB(sCustomer);

//                var processConfig = dbProcess.HCSSYS_ProcessConfigs.FirstOrDefault();
//                var arrPartFunction = sFunctionID.Split('.');
//                if (arrPartFunction.Length >= 3)
//                {
//                    processConfig = (String.IsNullOrEmpty(sTypeProcess))
//                        ? dbProcess.HCSSYS_ProcessConfigs.FirstOrDefault(p => p.FunctionID == arrPartFunction[0])
//                        : dbProcess.HCSSYS_ProcessConfigs.FirstOrDefault(p => p.FunctionID == arrPartFunction[0] && p.TypeCode == sTypeProcess);
//                }
//                else
//                {
//                    processConfig = (String.IsNullOrEmpty(sTypeProcess))
//                        ? dbProcess.HCSSYS_ProcessConfigs.FirstOrDefault(p => p.FunctionID == sFunctionID)
//                        : dbProcess.HCSSYS_ProcessConfigs.FirstOrDefault(p => p.FunctionID == sFunctionID && p.TypeCode == sTypeProcess);
//                }

//                bool IsLTRequiredEmps = false;
//                if (processConfig != null)
//                {
//                    //tinh lai so Procees
//                    if (dataLength < processConfig.TotalEmployeePerProcess * processConfig.TotalProcess)
//                    {
//                        IsLTRequiredEmps = true;
//                        if (dataLength % processConfig.TotalEmployeePerProcess > 0)
//                        {
//                            iNumberProcess = dataLength / processConfig.TotalEmployeePerProcess + 1;
//                        }
//                        else
//                        {
//                            iNumberProcess = dataLength / processConfig.TotalEmployeePerProcess;
//                        }
//                    }
//                    else
//                    {
//                        IsLTRequiredEmps = false;
//                        if (dataLength > iNumberProcess)
//                        {
//                            iNumberProcess = (dataLength >= processConfig.TotalProcess) ? processConfig.TotalProcess : 1;
//                            iItemPerProcess = (processConfig.TotalEmployeePerProcess > 0
//                                && dataLength >= (iNumberProcess * processConfig.TotalEmployeePerProcess))
//                                ? processConfig.TotalEmployeePerProcess : 1;
//                        }
//                    }
//                }

//                #endregion

//                #region Tạo biến chứa số lượng và danh sách process

//                //Tạo biến lưu số lượng process
//                if (hashMultiProcess[ProcessName + "_Count"] == null)
//                {
//                    hashMultiProcess.Add(ProcessName + "_Count", iNumberProcess);
//                }
//                else
//                {
//                    hashMultiProcess[ProcessName + "_Count"] = iNumberProcess;
//                }
//                //Tạo biến chứa danh sách process
//                if (hashMultiProcess[ProcessName] == null)
//                {
//                    hashMultiProcess.Add(ProcessName, new List<MultiProcess>());
//                }
//                else
//                {
//                    hashMultiProcess[ProcessName] = new List<MultiProcess>();
//                }

//                #endregion

//                var processlist = (List<MultiProcess>)hashMultiProcess[ProcessName];

//                for (int i = 0; i < iNumberProcess; i++)
//                {
//                    #region Tạo process

//                    MultiProcess oProcess = new MultiProcess();

//                    if (i < iNumberProcess - 1)
//                    {
//                        //Số lượng item cần xử lý trên 1 process
//                        int numberItem = dataLength / iNumberProcess;

//                        oProcess.FromIndex = (i * numberItem);
//                        oProcess.ToIndex = (i * numberItem) + numberItem;
//                    }
//                    else
//                    {
//                        //Số lượng item cần xử lý trên 1 process
//                        int numberItem = dataLength / iNumberProcess + dataLength % iNumberProcess;

//                        oProcess.FromIndex = (i * (dataLength / iNumberProcess));
//                        oProcess.ToIndex = (i * (dataLength / iNumberProcess)) + numberItem;
//                    }

//                    if (oProcess.hashData["data"] == null)
//                    {
//                        oProcess.hashData.Add("data", DataList);
//                    }
//                    else
//                    {
//                        oProcess.hashData["data"] = DataList;
//                    }

//                    oProcess.DoWork = (Hashtable hashData, int iFrom, int iTo) =>
//                    {
//                        try
//                        {
//                            List<object> objs = (List<object>)hashData["data"];

//                            int iNumberItemPerProcess = iItemPerProcess;
//                            if (IsLTRequiredEmps)
//                            {
//                                iNumberItemPerProcess = iTo - iFrom;
//                            }

//                            for (int k = iFrom; k < iTo; k += iNumberItemPerProcess)
//                            {
//                                if (!stopProcess)
//                                {
//                                    List<object> lstObj = new List<object>();
//                                    for (int z = k; z < k + iNumberItemPerProcess; z++)
//                                    {
//                                        if (z < iTo)
//                                        {
//                                            lstObj.Add(objs.ElementAt<object>(z));
//                                        }
//                                    }

//                                    var task = Task.Run(() =>
//                                    {
//                                        //Chạy hàm được định nghĩa
//                                        DoAction.Invoke(lstObj);
//                                        //Chờ 100ms hàm Dispose của DbContext giúp thu hồi tài nguyên hệ thống. (Tránh chiếm RAM và CPU)
//                                        Task.Delay(300).Wait();
//                                    });
//                                    task.Wait();

//                                    //Tính % cho từng process
//                                    for (int z = k; z < k + iNumberItemPerProcess; z++)
//                                    {
//                                        if (z < iTo)
//                                        {
//                                            oProcess.ReportPercent((((z + 1) - iFrom) * 100) / (iTo - iFrom));
//                                        }
//                                    }
//                                }
//                                else
//                                {
//                                    oProcess.ReportError(error);
//                                }
//                            }

//                        }
//                        catch (Exception ex)
//                        {
//                            string msgError = (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message)) ? ex.InnerException.Message : ex.Message;
//                            log.Error(ex);
//                            oProcess.ReportError(msgError);
//                            stopProcess = true;
//                            error = msgError;
//                        }

//                    };
//                    processlist.Add(oProcess);

//                    #endregion
//                }

//                //Lưu danh sách process vào hashtable
//                hashMultiProcess[ProcessName] = processlist;

//                //Mảng đếm số process đã hoàn tất
//                bool[] bProcessDone = new bool[processlist.Count];
//                log.Info(processlist);
//                //Thực thi các process
//                Parallel.For(0, processlist.Count, i =>
//                {
//                    bProcessDone[i] = false;
//                    processlist[i].RunAsync();
//                });
//                //for (int i = 0; i < processlist.Count; i++)
//                //{
//                //    bProcessDone[i] = false;
//                //    processlist[i].RunAsync();
//                //}

//                int iCompletedProcess = bProcessDone.Count(p => p == true);
//                while (iCompletedProcess < processlist.Count)
//                {
//                    for (int i = 0; i < processlist.Count; i++)
//                    {
//                        if (processlist.ElementAt(i).IsDone)
//                        {
//                            bProcessDone[i] = true;
//                            iCompletedProcess = bProcessDone.Count(p => p == true);
//                        }
//                    }
//                }


//                GC.Collect();
//            }
//        }
//        /// <summary>
//        /// Lấy thông tin tỉ lệ % của tiến trình hiện tại
//        /// </summary>
//        /// <returns>List MultiProcess</returns>
//        public object GetPercentProcess()
//        {
//            //Loi lau lau bi
//            //  var _c = hashMultiProcess[ProcessName + "_Count"];
//            int count = ((int)hashMultiProcess[ProcessName + "_Count"]);

//            List<GetPercentProcessResponse> listResponse = new List<GetPercentProcessResponse>();

//            if (hashMultiProcess[ProcessName] != null)
//            {
//                var processlist = (List<MultiProcess>)hashMultiProcess[ProcessName];

//                bool bError = false;
//                for (int i = 0; i < count; i++)
//                {
//                    if (processlist.Count > i)
//                    {
//                        var oProcess = processlist.ElementAt(i);
//                        if (oProcess.pInfo.Status != MultiProcessStatus.Error)
//                        {
//                            listResponse.Add(new GetPercentProcessResponse()
//                            {
//                                Percent = oProcess.pInfo.Percent,
//                                Status = oProcess.pInfo.Status,
//                                IsDone = oProcess.IsDone
//                            });
//                        }
//                        else
//                        {
//                            bError = true;
//                            listResponse.Add(new GetPercentProcessResponse()
//                            {
//                                Error = new XError()
//                                {
//                                    Message = oProcess.pInfo.Error
//                                },
//                                Percent = oProcess.pInfo.Percent,
//                                Status = oProcess.pInfo.Status,
//                                IsDone = oProcess.IsDone
//                            });
//                        }
//                    }
//                }
//                //Dừng tất cả nếu 1 process bị lỗi
//                if (bError)
//                {
//                    foreach (var item in listResponse)
//                    {
//                        item.IsDone = true;
//                    }
//                }

//                int countDone = listResponse.Count(p => p.IsDone);
//                if (countDone == processlist.Count)
//                {
//                    hashMultiProcess.Remove(ProcessName);
//                    hashMultiProcess.Remove(ProcessName + "_Count");
//                }
//            }

//            return listResponse;
//        }
//        /// <summary>
//        /// Lấy số lượng process
//        /// </summary>
//        /// <returns>{ NumberProcess = Số lượng process }</returns>
//        public object GetNumberProcess()
//        {
//            //Chờ tối đa 30s để đọc dữ liệu tạo số lượng process
//            var watch = System.Diagnostics.Stopwatch.StartNew();
//            while (hashMultiProcess[ProcessName + "_Count"] == null)
//            {
//                var elapsedMs = watch.ElapsedMilliseconds;
//                if (elapsedMs >= 30000)
//                {
//                    watch.Stop();
//                    hashMultiProcess[ProcessName + "_Count"] = 0;
//                    break;
//                }
//            }

//            int count = (int)hashMultiProcess[ProcessName + "_Count"];
//            return new { NumberProcess = count };
//        }
//    }
//}

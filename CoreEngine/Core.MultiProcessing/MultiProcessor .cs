//using LV.HCS.MultiProcess;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace Core.MultiProcessing
//{

//    public interface IMultiProcessor
//    {
//        // Define methods and properties for the multi-processor interface
//    }


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

//    public class MultiProcess<T>
//    {
//        public List<T> Data { get; set; } = new List<T>();

//        public int FromIndex { get; set; } = 0;

//        public int ToIndex { get; set; } = 0;


//        public Action<List<T>, int, int> Action;

//        public bool IsCompleted { get; set; } = false;



//        public async Task RunTaskAsync()
//        {
//            Action(Data, FromIndex, ToIndex);
//            GC.Collect();
//        }
//    }
//    public class MultiProcessor<T> : IMultiProcessor
//    {
//        private readonly string processName;
//        private readonly List<T> dataList;
//        private readonly Action<List<T>> doAction;

//        public MultiProcessor(string processName, List<T> dataList, Action<List<T>> doAction)
//        {
//            this.processName = processName;
//            this.dataList = dataList;
//            this.doAction = doAction;
//        }


//        public async Task DoWork()
//        {




//        }






//    }
//}

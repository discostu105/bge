namespace BrowserGameEngine.Shared {
	public class WorkerAssignmentViewModel {
		public int TotalWorkers { get; set; }
		public int MineralWorkers { get; set; }
		public int GasWorkers { get; set; }
		public int IdleWorkers => TotalWorkers - MineralWorkers - GasWorkers;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.Persistence {
	public class FileStorage : IBlobStorage {
		private readonly DirectoryInfo directory;

		public FileStorage(DirectoryInfo directory) {
			this.directory = directory;
			EnsureDirectory();
		}

		private void EnsureDirectory() {
			directory.Create();
		}

		private FileInfo GetFile(string name) {
			return new FileInfo(Path.Combine(directory.FullName, name));
		}

		public bool Exists(string name) => GetFile(name).Exists;

		public async Task<byte[]> Load(string name) {
			var file = GetFile(name);
			if (!file.Exists) throw new FileNotFoundException("File not found", file.FullName);
			return await File.ReadAllBytesAsync(file.FullName);
		}

		public async Task Store(string name, byte[] blob) {
			var file = GetFile(name);
			file.Directory?.Create();
			var tmpPath = file.FullName + ".tmp";
			await File.WriteAllBytesAsync(tmpPath, blob);
			File.Move(tmpPath, file.FullName, overwrite: true);
		}

		public IEnumerable<string> List(string folderPrefix) {
			var subDir = new DirectoryInfo(Path.Combine(directory.FullName, folderPrefix));
			if (!subDir.Exists) return Enumerable.Empty<string>();
			return subDir.GetFiles().Select(f => folderPrefix + "/" + f.Name);
		}

		public Task Delete(string name) {
			var file = GetFile(name);
			if (file.Exists) file.Delete();
			return Task.CompletedTask;
		}
	}
}

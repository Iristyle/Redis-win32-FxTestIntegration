using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

namespace RedisIntegration
{
	/// <summary>	A helper class that can launch Redis instances under test.  </summary>
	/// <remarks>	8/3/2011. </remarks>
	public static class HostManager
	{
		private static ConcurrentDictionary<Connection, HostProcessController> currentHosts 
			= new ConcurrentDictionary<Connection, HostProcessController>();

		/// <summary>	Establishes a running server on the given port, with a visible Redis process window.  Issues a FLUSHALL command after starting up the first time. </summary>
		/// <param name="port">   	The port (Redis default is 6379). </param>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection RunInstanceWithVisibleWindow(int port)
		{
			return RunInstance(port, true);
		}

		/// <summary>	Establishes a running server on the given port, with a hidden Redis process window.  Issues a FLUSHALL command after starting up the first time. </summary>
		/// <param name="port">   	The port (Redis default is 6379). </param>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection RunInstance(int port)
		{
			return RunInstance(port, false);
		}

		/// <summary>	Establishes a running server on default port 6379, with a visible Redis process window.  Issues a FLUSHALL command after starting up the first time. </summary>
		/// <remarks>	The port defaults to 6379 (Redis default). </remarks>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection RunInstanceWithVisibleWindow()
		{
			return RunInstance(6379, true);
		}

		/// <summary>	Establishes a running server on default port 6379, with a hidden Redis process window.  Issues a FLUSHALL command after starting up the first time. </summary>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection RunInstance()
		{
			return RunInstance(6379, false);
		}

		private static Connection RunInstance(int port, bool visible)
		{
			var connection = new Connection("127.0.0.1", port);
			currentHosts.GetOrAdd(connection, c => StartRedisInstance(c.Host, c.Port, visible));
			return connection;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "The HostController now owns the Process and its finalizer disposes it")]
		private static HostProcessController StartRedisInstance(string host, int port, bool visible)
		{
			var asm = typeof(HostManager).Assembly;
			
			var redisServerFileName = "redis-server.exe";
			var redisConfFileName = "redis.conf";
			string targetFilePath = Path.Combine(Path.GetTempPath(), "RedisIntegration");
			if(!Directory.Exists(targetFilePath))
				Directory.CreateDirectory(targetFilePath);

			var resNameServer = asm.GetManifestResourceNames().First(s => s.ToLower().Contains(redisServerFileName));
			var resNameConf = asm.GetManifestResourceNames().First(s => s.ToLower().Contains(redisConfFileName));
			
			Dictionary<string, string> resToFile = new Dictionary<string,string>();
			resToFile.Add(resNameServer, redisServerFileName);
			resToFile.Add(resNameConf, redisConfFileName);

			foreach(var res in new []{resNameServer, resNameConf})
			{
				using(var reader = new BinaryReader(asm.GetManifestResourceStream(res)))
				{
					File.WriteAllBytes(Path.Combine(targetFilePath, resToFile[res]), reader.ReadBytes((int)reader.BaseStream.Length));
				}
			}

			var redisServerFullPath = Path.Combine(targetFilePath, redisServerFileName);
			var redisConfFullPath = Path.Combine(targetFilePath, redisConfFileName);

			if(!File.Exists(redisServerFullPath))
				throw new FileNotFoundException(string.Format("redis-server.exe was not found at expected path: {0}", redisServerFullPath));
			if(!File.Exists(redisConfFullPath))
				throw new FileNotFoundException(string.Format("redis.conf was not found at expected path: {0}", redisConfFullPath));

			string databaseFilePath = Path.Combine(targetFilePath, Path.GetFileName(Path.GetTempFileName().Replace(".tmp", ".rdb")));

			string configuration = File.ReadAllText(redisConfFullPath);

			//put in our specific environment info
			configuration = Regex.Replace(configuration, "port 6379", String.Format(CultureInfo.InvariantCulture, "port {0}", port));
			configuration = Regex.Replace(configuration, "databases 16", "databases 1");
			configuration = Regex.Replace(configuration, @"dbfilename dump\.rdb", String.Format(CultureInfo.InvariantCulture, "dbfilename {0}", Path.GetFileName(databaseFilePath)));
			configuration = Regex.Replace(configuration, @"dir \./", String.Format(CultureInfo.InvariantCulture, "dir {0}", targetFilePath));

			File.WriteAllText(redisConfFullPath, configuration);

			var processInfo = new ProcessStartInfo(redisServerFullPath, redisConfFullPath) { WorkingDirectory = targetFilePath };
			if (!visible)
			{
				processInfo.UseShellExecute = false;
				processInfo.CreateNoWindow = true;
			}

			var process = Process.Start(processInfo);

			Thread.Sleep(100);

			if(process.HasExited)
			{
				string message = string.Format("Redis-server.exe failed to start.  Exit code: {0}", process.ExitCode);

				throw new Exception(message);
			}

			//totally clear out any junk we might have in there from old starts with a FLUSHALL
			using (var client = new TcpClient(host, port))
			{
				//http://redis.io/topics/protocol
				var flushAll = Encoding.ASCII.GetBytes("*1\r\n$8\r\nFLUSHALL\r\n");
				client.GetStream().Write(flushAll, 0, flushAll.Length);
			}

			return new HostProcessController(databaseFilePath, redisConfFullPath, host, port, process);
		}
	}
}
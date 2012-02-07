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
			string tempPath = Path.GetTempPath();

			string sourceDirectory = string.Empty;
			//sniff up the directory tree just in case something weird is going on
			var asm = typeof(HostManager).Assembly;
			for (int i = 1; i < 6; ++i)
			{
				string machineBits = (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86" && Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") == null) 
					? "32bit" : "64bit";
				var pathBits = new[] { Path.GetDirectoryName(asm.Location) }.Concat(Enumerable.Repeat("..", i))
					.Concat(new[] {"packages", "RedisIntegration." + asm.GetName().Version.ToString(4), "tools", machineBits });
				sourceDirectory = Path.Combine(pathBits.ToArray());

				if (Directory.Exists(sourceDirectory))
					break;
			}

			//copy to our temp folder
			foreach (var fileName in new [] { "redis-server.exe" })
				File.Copy(Path.Combine(sourceDirectory, fileName), Path.Combine(tempPath, fileName), true);

			string databaseFilePath = Path.GetTempFileName().Replace(".tmp", ".rdb");

			string configuration = File.ReadAllText(Path.Combine(sourceDirectory, "redis.conf"));

			//put in our specific environment info
			configuration = Regex.Replace(configuration, "port 6379", String.Format(CultureInfo.InvariantCulture, "port {0}", port));
			configuration = Regex.Replace(configuration, "databases 16", "databases 1");
			configuration = Regex.Replace(configuration, @"dbfilename dump\.rdb", String.Format(CultureInfo.InvariantCulture, "dbfilename {0}", Path.
			GetFileName(databaseFilePath)));
			configuration = Regex.Replace(configuration, @"dir \./", String.Format(CultureInfo.InvariantCulture, "dir {0}", tempPath));

			string configPath = Path.GetTempFileName().Replace(".tmp", ".conf");
			File.WriteAllText(configPath, configuration);

			var processInfo = new ProcessStartInfo(Path.Combine(sourceDirectory, "redis-server.exe"), configPath) { WorkingDirectory = tempPath };
			if (!visible)
			{
				processInfo.UseShellExecute = false;
				processInfo.CreateNoWindow = true;
			}

			var process = Process.Start(processInfo);

			//totally clear out any junk we might have in there from old starts with a FLUSHALL
			using (var client = new TcpClient(host, port))
			{
				//http://redis.io/topics/protocol
				var flushAll = Encoding.ASCII.GetBytes("*1\r\n$8\r\nFLUSHALL\r\n");
				client.GetStream().Write(flushAll, 0, flushAll.Length);
			}			

			return new HostProcessController(databaseFilePath, configPath, host, port, process);
		}
	}
}
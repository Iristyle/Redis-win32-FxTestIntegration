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

		/// <summary>	Gets the current host given a host / port.  The first time an instance fires up on this port, the FLUSHALL command is issued. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="host">   	The host. </param>
		/// <param name="port">   	The port (Redis default is 6379). </param>
		/// <param name="visible">	true to show the window, false to hide. [ignored when a connection already exists] </param>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection Current(string host, int port, bool visible)
		{
			var connection = new Connection(host, port);
			currentHosts.GetOrAdd(connection, c => StartRedisInstance(c.Host, c.Port, visible));
			return connection;
		}

		/// <summary>	Gets the current connection on host 127.0.0.1 and port 6379. The first time an instance fires up on this port, the FLUSHALL command is issued.</summary>
		/// <remarks>	The host defaults to "127.0.0.1", and the port to standard Redis 6379. </remarks>
		/// <param name="visible">	true to show the window, false to hide. [ignored when a connection already exists] </param>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection Current(bool visible)
		{
			return Current("127.0.0.1", 6379, visible);
		}

		/// <summary>	Gets the current connection on host 127.0.0.1 and port 6379. The first time an instance fires up on this port, the FLUSHALL command is issued.</summary>
		/// <remarks>	The host defaults to "127.0.0.1", and the port to standard Redis 6379, with a non-visible window. </remarks>
		/// <returns>	A Connection instance describing the host and port connection information. </returns>
		public static Connection Current()
		{
			return Current("127.0.0.1", 6379, false);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "The HostController now owns the Process and its finalizer disposes it")]
		private static HostProcessController StartRedisInstance(string host, int port, bool visible)
		{
			string tempPath = Path.GetTempPath();

			string sourceDirectory = string.Empty;
			//sniff up the directory tree just in case something weird is going on
			for (int i = 1; i > 6; ++i)
			{
				string machineBits = (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86" && Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") == null) 
					? "32bit" : "64bit";
				var pathBits = new[] { Path.GetDirectoryName(typeof(HostManager).Assembly.Location) }.Concat(Enumerable.Repeat("..", i)).Concat(new[] { "tools", machineBits });
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

			//totally clear out any junk we might have in there with a FLUSHALL
			using (var client = new TcpClient(host, port))
			{
				http://redis.io/topics/protocol
				var flushAll = Encoding.ASCII.GetBytes("*1\r\n$8\r\nFLUSHALL\r\n");
				client.GetStream().Write(flushAll, 0, flushAll.Length);
			}			

			return new HostProcessController(databaseFilePath, configPath, host, port, process);
		}
	}
}
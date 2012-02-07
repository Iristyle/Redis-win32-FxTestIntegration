using System;

namespace RedisIntegration
{
	/// <summary>	Defines immutable Redis connection information.  </summary>
	/// <remarks>	8/3/2011. </remarks>
	public class Connection : IEquatable<Connection>
	{
		/// <summary>	Gets the port. </summary>
		/// <value>	The port. </value>
		public int Port { get; private set; }
		
		/// <summary>	Gets the host address. </summary>
		/// <value>	The host. </value>
		public string Host { get; private set; }
		
		/// <summary>	Constructor. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="port">	The port. </param>
		/// <param name="host">	The host. </param>
		public Connection(string host, int port)
		{
			Port = port;
			Host = host;
		}

		/// <summary>	Tests if this RedisConnection is considered equal to another. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="other">	The redis connection to compare to this object. </param>
		/// <returns>	true if the objects are considered equal, false if they are not. </returns>
		public bool Equals(Connection other)
		{
			if (null == other) { return false; }
			
			return this.Host.Equals(other.Host, StringComparison.OrdinalIgnoreCase)
				&& this.Port == other.Port;
		}
	}
}
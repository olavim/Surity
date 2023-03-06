using System;
using System.Net.Sockets;
using System.Text;

namespace Surity
{
	public class TestLogger
	{
		private readonly Socket socket;

		public TestLogger(Socket socket)
		{
			this.socket = socket;
		}

		public void Log(string msg)
		{
			this.socket.Send(Encoding.UTF8.GetBytes(msg), SocketFlags.None);
			Console.WriteLine(msg);
		}
	}
}

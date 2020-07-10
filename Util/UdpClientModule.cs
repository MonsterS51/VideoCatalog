using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VideoCatalog.Util {
	public class UdpClientModule {

		public void TestUdp(string username, string pass, string apiVer) {
			string auth = @"AUTH user="+username.ToLower()+ @"&pass="+ pass + "&protover=3&client=vidcat&clientver=1";
			string authResult = SendUdp(auth);

			string[] col = authResult.Split(' ');
			string sessionKey = col[1];
			string search = @"ANIME aname=BaBuKa&amask=b2f0e0fc000000&s=" + sessionKey;
			SendUdp(search);

			SendUdp("LOGOUT s=" + sessionKey);
		}

		public string SendUdp(string message) {
			Console.WriteLine("Send: " + message);
			UdpClient udpClient = new UdpClient(9000);
			string returnData = null;
			try {
				udpClient.Connect("api.anidb.net", 9000);

				//IPEndPoint object will allow us to read datagrams sent from any source.
				IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

				// Sends a message to the host to which you have connected.
				Byte[] sendBytes = Encoding.ASCII.GetBytes(message);

				udpClient.Send(sendBytes, sendBytes.Length);

				// Blocks until a message returns on this socket from a remote host.
				Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
				returnData = Encoding.ASCII.GetString(receiveBytes);

				// Uses the IPEndPoint object to determine which of these two hosts responded.
				Console.WriteLine("Return: " + returnData);
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
			} finally {
				udpClient.Close();
			}

			return returnData.ToString();
		}


	}
}

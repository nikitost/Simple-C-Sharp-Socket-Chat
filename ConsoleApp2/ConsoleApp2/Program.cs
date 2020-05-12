using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp2
{
    class Program
    {
        private string UserName;
        private string host;
        private const int port = 13001;
        private TcpClient Client;
        private NetworkStream Stream;

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Start();
        }

        private void Start()
        {
            Console.WriteLine("Введите имя: ");
            UserName = Console.ReadLine();
            Console.WriteLine("Введите адрес сервера: ");
            host = Console.ReadLine();
            Client = new TcpClient();
            try
            {
                Client.Connect(host, port);
                Stream = Client.GetStream();

                string Message = UserName;
                byte[] buffer = Encoding.Unicode.GetBytes(Message);
                Stream.Write(buffer, 0, buffer.Length);

                Thread RecieveThread = new Thread(new ThreadStart(RecieveMessage));
                RecieveThread.Start();

                SendMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }
        private void SendMessage()
        {
            while (true)
            {
                string Message = Console.ReadLine();
                byte[] buffer = Encoding.Unicode.GetBytes(Message);
                Stream.Write(buffer, 0, buffer.Length);
            }
        }

        private void RecieveMessage()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[256];
                    StringBuilder Builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = Stream.Read(buffer, 0, buffer.Length);
                        Builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
                    } while (Stream.DataAvailable);

                    string Message = Builder.ToString();
                    Console.WriteLine(Message);
                }
                catch
                {
                    Console.WriteLine("Подключение прервано");
                    Console.ReadLine();
                    Disconnect();
                }
            }
        }

        private void Disconnect()
        {
            if (Stream != null)
            {
                Stream.Close();
            }
            if (Client != null)
            {
                Client.Close();
            }
            Environment.Exit(0);
        }

    }
}

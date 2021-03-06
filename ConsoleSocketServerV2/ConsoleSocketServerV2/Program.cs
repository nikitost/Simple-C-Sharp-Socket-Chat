﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;

namespace ConsoleSocketServerV2
{
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        protected internal string UserName { get; private set; }
        TcpClient Client;
        ServerObject Server;

        public ClientObject(TcpClient client, ServerObject server)
        {
            Id = Guid.NewGuid().ToString();
            Client = client;
            Server = server;
            server.AddConection(this);
        }

        public void Process()
        {
            try
            {
                Stream = Client.GetStream();
                string Message = GetMessage();
                UserName = Message;

                Message = "[" + Convert.ToString(System.DateTime.Now) + "] " + UserName + " connected.";
                Server.BroadcastMessage(Message, this.Id);
                Console.WriteLine(Message);
                while (true)
                {
                    try
                    {
                        Message = GetMessage();
                        if (Message != "/users")
                        {
                            Message = "[" + Convert.ToString(System.DateTime.Now) + "] " + String.Format("{0}: {1}", UserName, Message);
                            Console.WriteLine(Message);
                            Server.BroadcastMessage(Message, this.Id);
                        }
                        else
                        {
                            Console.WriteLine(Message + "для" + UserName);
                            Server.Command(Message, this.Id);
                        }
                    }
                    catch
                    {
                        Message = "[" + Convert.ToString(System.DateTime.Now) + "] " + UserName + " disconnected.";
                        Console.WriteLine(Message);
                        Server.BroadcastMessage(Message, this.Id);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Server.RemoveConnection(this.Id);
                Close();
            }
        }

        string GetMessage()
        {
            byte[] buffer = new byte[64];
            StringBuilder Builder = new StringBuilder();
            int bytes = 0;
            do
            {
                bytes = Stream.Read(buffer, 0, buffer.Length);
                Builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
            } while (Stream.DataAvailable);
            return Builder.ToString();
        }

        protected internal void Close()
        {
            if (Stream != null)
            {
                Stream.Close();
            }
            if (Client != null)
            {
                Client.Close();
            }
        }
    }

    public class ServerObject
    {
        static TcpListener Listener;
        List<ClientObject> Clients = new List<ClientObject>();

        protected internal void AddConection(ClientObject clientObject)
        {
            Clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            ClientObject Client = Clients.FirstOrDefault(x => x.Id == id);
            if (Client != null)
            {
                Clients.Remove(Client);
            }
        }
        protected internal void Listen()
        {
            try
            {
                Listener = new TcpListener(IPAddress.Any, 13001);
                Listener.Start();
                Console.WriteLine("Server start. Waiting for connections...");

                while (true)
                {
                    TcpClient Client = Listener.AcceptTcpClient();

                    ClientObject CO = new ClientObject(Client, this);
                    Thread ClientThread = new Thread(new ThreadStart(CO.Process));
                    ClientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }
        protected internal void Command(string message, string id)
        {
            string command = "";
            if (message == "/users") //если в сообщении команда
            {
                command = "/users"; //установить команду
                message = ""; 
                for (int i = 0; i < Clients.Count; i++)
                {
                    message += Clients[i].UserName + "; "; //формирование списка юзеров
                }
            }
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            if (command == "/users")
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    if (Clients[i].Id == id) //отправка сообщения-списка
                        Clients[i].Stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        protected internal void BroadcastMessage(string message, string id)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(message);
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    //if (Clients[i].Id != id)
                    //{
                    try
                    {
                        Clients[i].Stream.Write(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        continue;
                    }
                    //}
                }
            }
        }
        protected internal void Disconnect()
        {
            Listener.Stop();

            for (int i = 0; i < Clients.Count; i++)
            {
                Clients[i].Close();
            }
            Environment.Exit(0);
        }
    }
    class Program
    {
        static ServerObject Server;
        static Thread ListenThread;
        static void Main(string[] args)
        {
            try
            {
                Server = new ServerObject();
                ListenThread = new Thread(new ThreadStart(Server.Listen));
                ListenThread.Start();
            }
            catch (Exception ex)
            {
                Server.Disconnect();
                Console.WriteLine(ex.Message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;

namespace TCPServer
{
    class Server
    {
        private TcpListener server;
        private Dictionary<string, TcpClient> userNames = new Dictionary<string, TcpClient>();
        private static Queue<string> messages = new Queue<string>();
        public readonly string ChatRoom;
        public readonly int Port;
        private string user;

        public Server(string chatRoom, int port)
        {
            ChatRoom = chatRoom;
            Port = port;
            server = new TcpListener(IPAddress.Any, 8008);
        }
        public void StartServer()
        {
            try
            {
                Console.WriteLine("Server {0} is starting on port {1}", ChatRoom, Port);
                server.Start();
                
                int counter = 0;
                while (true)
                {
                    Console.Write("Connecting...");
                    counter += 1;
                    TcpClient client = new TcpClient();
                    client = server.AcceptTcpClient();
                    Thread newUser = new Thread(() => handleNewUser(client));
                    newUser.Start();
                    //handleDisconnect();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error...");
            }

            finally
            {
                Console.WriteLine("Goodbye");
                Console.ReadLine();
            }
        }
        private void handleNewUser(TcpClient client)
        {
            try
            {
                EndPoint endPoint = client.Client.RemoteEndPoint;
                Console.WriteLine("New connection established from {0}", endPoint);
                StreamReader reader = new StreamReader(client.GetStream()); //string userMessage = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream());
                user = getUserName(client, reader, writer);
                userNames.Add(user, client);
                writer.WriteLine("Welcome to {0}, {1}", ChatRoom, user);
                writer.Flush();
                Broadcast("User " + user + " has joined " + ChatRoom + ".", reader, writer);
                Thread StartChat = new Thread(() => startChat(client, reader, writer));
                StartChat.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private string getUserName(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            writer.WriteLine("Enter a Username: \n");
            writer.Flush();

            string userMessage = reader.ReadLine();
            Console.WriteLine("User {0} has connected", userMessage);

            return userMessage;
        }
        private void startChat(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            try
            {
                Thread receiveMessageThread = new Thread(() => receiveMessages(client, reader, writer));
                receiveMessageThread.Start();
                Thread sendMessageThread = new Thread(() => sendMessages(client, reader, writer));
                sendMessageThread.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void Broadcast(string v, StreamReader reader, StreamWriter writer)
        {
            try
            {
                foreach (KeyValuePair<string, TcpClient> user in userNames)
                {
                    writer.Write(v);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void receiveMessages(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            try
            {
                foreach (KeyValuePair<string, TcpClient> user in userNames)
                {
                    if (reader != null)
                    {
                        ServicePointManager.Expect100Continue = false;
                        string userMessage = reader.ReadLine(); //break point
                        messages.Enqueue(userMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void sendMessages(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            try
            {
                foreach (KeyValuePair<string, TcpClient> user in userNames)
                {
                    using (writer)
                    {
                        while (messages.Count != 0)
                        {
                            messages.Dequeue();
                            Broadcast(user + ": ", reader, writer);
                            writer.Flush();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void handleDisconnect(TcpClient client, StreamReader reader, StreamWriter writer)
        {
            foreach (KeyValuePair<string, TcpClient> user in userNames)
            {
                if (reader.Equals("exit"))
                {
                    Broadcast("User " + client + " has left " + ChatRoom + ".", reader, writer);
                }
            }
            writer.Close();
            reader.Close();
            userNames.Remove(user);
        }
    }
}

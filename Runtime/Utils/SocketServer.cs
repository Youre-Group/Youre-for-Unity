using System;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class SocketServer
    {
        
        
        
        
        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> MessageReceived;

        private bool urlSended = false;
        private TcpClient client;
        private NetworkStream stream;
        private bool isConnectedToServer = false;
        private string serverIP = "127.0.0.1"; // IP of the macOS server
        private int serverPort = 8080; // Port of the macOS server

        public void Start()
        {
            ConnectToServer();
        }


        private async void ExecuteAfterDelay(float delayInSeconds, Action action)
        {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
            action?.Invoke();
        }
        
        public void Disconnect()
        {
            DisconnectFromServer();
        }

        /// <summary>
        /// Connect to the SocketServer.
        /// </summary>
        private void ConnectToServer()
        {
            Debug.Log("Try to the server connect!");
            
            try
            {
                if (isConnectedToServer == false)
                {
                    client = new TcpClient(serverIP, serverPort);
                    stream = client.GetStream();
                    Debug.Log("Connect to the server Success!");
                    isConnectedToServer = true;

                    if (urlSended == false)
                    {
                        Connected?.Invoke();
                    }
                    
                    ListenForMessages();
                }
            }
            catch (Exception e)
            {
                Debug.Log("Connect to the server Failed! "+e.Message);
                ExecuteAfterDelay(0.5f, ()=>
                {
                    ConnectToServer();
                });
            }
        }

        /// <summary>
        /// Send a message to the SocketServer.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessageToServer(string message)
        {
            if (isConnectedToServer)
            {
                if (client == null || !client.Connected)
                {
                    Debug.LogWarning("Not connected to the server. Message not sent.");
                    return;
                }

                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    //   stream.Dispose();
                    urlSended = true;
                    Debug.Log("Message sent: " + message);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error sending message: " + e.Message);
                }

                DisconnectFromServer();

                ExecuteAfterDelay(0.5f, ()=>
                {
                    Debug.Log("Second Connect to the server!");

                    ConnectToServer();
                });
            }
        }


        /// <summary>
        /// Disconnect from the SocketServer.
        /// </summary>
        private void DisconnectFromServer()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
            
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
            
            isConnectedToServer = false;
            Disconnected?.Invoke();
            Debug.Log("Disconnected from the server.");
        }
        
        

        
        /// <summary>
        /// Continuously listen for messages from the server.
        /// </summary>
        private async void ListenForMessages()
        {
            if (!isConnectedToServer) return;

            try
            {
                while (isConnectedToServer && client.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.Log("Message received: " + message);
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving message: " + e.Message);
                DisconnectFromServer();
                Disconnected?.Invoke();
            }
        }
    }
}
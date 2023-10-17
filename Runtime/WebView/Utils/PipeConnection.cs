using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using UnityEngine;
using System.Timers;

namespace WebView2Forms
{
    internal class PipeConnection
    {
        private NamedPipeClientStream _clientStream;
        private StreamReader _reader;
        private StreamWriter _writer;

        private bool _isConnected = false;
        private bool _connectingToServer = false;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> MessageReceived;
        private Task _t;
        private System.Threading.SynchronizationContext _mainThreadContext;

        private async void CheckPipe()
        {
            if (_clientStream == null)
                return;
            try
            {
                _clientStream.ReadByte();
            }
            catch {
            }
            if (!_clientStream.IsConnected)
            {
                _mainThreadContext.Post(_ => Disconnected?.Invoke(), null);
                StopConnection();
                return;
            }
            await Task.Delay(1000);
            CheckPipe();
        }


        private async void ConnectAsync()
        {
            while (_clientStream != null && !_clientStream.IsConnected)
            {
                try
                {
                    await _clientStream.ConnectAsync(1000);
                } catch
                {
                    Debug.Log("server not ready");
                }
                await Task.Delay(100);
            }

            if (_clientStream == null)
                return;

            _isConnected = true;

            Debug.Log("Connected");

            Connected?.Invoke();
            _mainThreadContext = System.Threading.SynchronizationContext.Current;
            _t = Task.Run(() => CheckPipe());
            ReadFromServer();
        }


        public void StartConnection()
        {
            StopConnection();

            if (_connectingToServer)
                return;

            _connectingToServer = true;

            _clientStream = new NamedPipeClientStream(".", "YoureLoginData123456", PipeDirection.InOut);
            _reader = new StreamReader(_clientStream, Encoding.UTF8);
            _writer = new StreamWriter(_clientStream, Encoding.UTF8);

            ConnectAsync();
        }

        public void StopConnection()
        {
            Debug.Log("StopConnection");
            try 
            {
                _isConnected = false;
                _connectingToServer = false;
                if(_clientStream != null)
                {
                    _clientStream.Close();
                    _clientStream.Dispose();
                    _clientStream = null;
                }
                if(_reader != null)
                {
                    _reader.Close();
                    _reader.Dispose();
                    _reader = null;
                }
                if (_writer != null)
                {
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
                
                _t?.Dispose();
                _t = null;
            } 
            catch(Exception e)
            {
                Debug.Log(e.Message);
                _clientStream = null;
            }
           
        }

        public void SendString(string message)
        {
            try
            {
                if (_isConnected == true)
                {
                    _writer.WriteLine(message);
                    _writer.Flush();
                }
                else
                {
                    Debug.Log($"PipeConnect SendString Not Possible is not Connected! Message= {message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending data to named pipe server: " + e.Message);
            }
        }

        private async void ReadFromServer()
        {
            try
            {
                string message = await _reader.ReadLineAsync();
                if (message != null)
                {
                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ReadFromServer - Error reading data from named pipe server: " + e.Message);
            }

            await Task.Delay(300);

            if (_isConnected == true) 
                ReadFromServer();
        }
    }
}


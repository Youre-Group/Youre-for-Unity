using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using UnityEngine;
using System.Timers;
using System.Threading;
using YourePlugin;

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
        private Task _t2;
        private System.Threading.SynchronizationContext _mainThreadContext;

        private async void CheckPipe()
        {

            while (_clientStream != null && _clientStream.IsConnected)
            {
                await Task.Delay(1000);
            }

            if (!_clientStream.IsConnected)
            {
                _mainThreadContext.Post(_ => Disconnected?.Invoke(), null);
                StopConnection();
            }

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
                    Youre.LogDebug("server not ready");
                }
                await Task.Delay(100);
            }

            if (_clientStream == null)
                return;

            _isConnected = true;

            Youre.LogDebug("Connected");

            Connected?.Invoke();
            _mainThreadContext = System.Threading.SynchronizationContext.Current;
            _t = Task.Run(() => CheckPipe());
            _t2 = Task.Run(() => ReadFromServer());
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
            try 
            {                

                _isConnected = false;
                _connectingToServer = false;
                if(_clientStream != null)
                {
                    _clientStream.Flush();
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
                    _writer.Flush();
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
                _t2?.Dispose();
                _t2 = null;
                _t?.Dispose();
                _t = null;
            } 
            catch(Exception e)
            {
                Youre.LogDebug(e.Message);
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
                    Youre.LogDebug($"PipeConnect SendString Not Possible is not Connected! Message= {message}");
                }
            }
            catch (Exception e)
            {
                Youre.LogDebug("Error sending data to named pipe server: " + e.Message);
            }
        }

        private async void ReadFromServer()
        {
            try
            {
                string message = await _reader.ReadLineAsync();
                if (message != null)
                {
                    _mainThreadContext.Post(_ => MessageReceived?.Invoke(message), null);
                }
            }
            catch (Exception e)
            {
                Youre.LogDebug("ReadFromServer - Error reading data from named pipe server: " + e.Message);
            }

            await Task.Delay(300);

            if (_isConnected == true) 
                ReadFromServer();
        }
    }
}


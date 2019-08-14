using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AsyncConnectionNS
{
    public class StateObject
    {
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
        //received data callback
        public Func<string> reCb;
    }

    public class DisconnectEventArgs : EventArgs
    {
        public bool requested;
    }

    public class LineReceivedEventArgs : EventArgs
    {
        public string line;
    }

    public class BytesReceivedEventArgs : EventArgs
    {
        public byte[] bytes;
        public int count;
    }

    public class AsyncConnection
    {

        protected static int _timeout = 10000;

        // ManualResetEvent instances signal completion.

        private string _host;
        private int _port;
        private volatile Socket socket;
        private volatile bool disconnecting;

        public event EventHandler<EventArgs> onConnected;
        public event EventHandler<DisconnectEventArgs> onDisconnected;
        public event EventHandler<LineReceivedEventArgs> lineReceived;
        public event EventHandler<BytesReceivedEventArgs> bytesReceived;

        public string lineBreak = "\r\n";
        public bool binaryMode;
        public bool reconnect;
        public int timeout = _timeout;

        public bool connected
        {
            get
            {
                return socket != null && socket.Connected;
            }
        }

        

        public bool connect()
        {
            if (_host != null && !_host.Equals( string.Empty ) && _port != 0)
                return connect(_host, _port);
            else
                return false;
        }

        public bool connect( string host, int port )
        {
            if (host == null || host.Equals(string.Empty) || port == 0 )
                return false;
            _host = host;
            _port = port;
            // Connect to a remote device.
            try
            {
                int retryCo = 0;

                while ((socket == null || !socket.Connected) && retryCo++ < 3)
                {
                    _connect().AsyncWaitHandle.WaitOne(timeout, true);

                    if (socket != null && !socket.Connected)
                    {
                        socket.Close();
                        System.Diagnostics.Debug.WriteLine("Connect timeout");
                    }
                    else
                        receive();
                }
                if (socket == null || !socket.Connected)
                {
                    System.Diagnostics.Debug.WriteLine("Retries limit reached. Connect failed");
                    if (reconnect)
                        asyncConnect();
                }
                return (socket != null && socket.Connected);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return false;
            }
        }

        public void connect(string host, int port, bool _async)
        {
            if (host == null || host.Equals(string.Empty) || port == 0)
                return;
            _host = host;
            _port = port;
            if (_async)
                asyncConnect();
            else
                connect(host, port);
        }

        public IAsyncResult _connect()
        {
            System.Diagnostics.Debug.WriteLine("Connecting to " + _host + ":" + _port.ToString());
            try
            {
                // Create a TCP/IP socket.
                socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                return socket.BeginConnect(_host, _port,
                    new AsyncCallback(connectCallback), null);
            } catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return null;
            }
        }

        public void asyncConnect()
        {
            IAsyncResult ar = _connect();
            if (ar != null)
                ThreadPool.RegisterWaitForSingleObject(ar.AsyncWaitHandle, new WaitOrTimerCallback(asyncConnectTimeout), null, timeout, true);
            else asyncConnect();
        }

        private void asyncConnectTimeout(object state, bool timedOut)
        {
            if (socket == null || !socket.Connected)
            {
                System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                System.Diagnostics.Debug.WriteLine(t.ToString());
                Debug.WriteLine("Async connect timeout");
                if (socket != null)
                    socket.Close();
                if (reconnect)
                    asyncConnect();
            }
            if (socket != null && socket.Connected)
                receive();
        }

        public void disconnect()
        {
            _disconnect(true);
        }

        private void _disconnect(bool requested)
        {
            if (disconnecting)
                return;
            System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
            System.Diagnostics.Debug.WriteLine(t.ToString());
            System.Diagnostics.Debug.WriteLine("disconnect");
            disconnecting = true;
            if (requested)
                reconnect = false;
            if (socket != null)
                socket.Close();
            onDisconnected?.Invoke(this, new DisconnectEventArgs { requested = requested });
            if (reconnect && !requested)
                asyncConnect();
        }

        private void connectCallback(IAsyncResult ar)
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    disconnecting = false;
                    socket.EndConnect(ar);
                    System.Diagnostics.Debug.WriteLine("Socket connected to " +
                        socket.RemoteEndPoint.ToString());

                    onConnected?.Invoke(this, new EventArgs { });
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private void receive()
        {
            try
            {
                //System.Diagnostics.Debug.WriteLine("receiving");
                // Create the state object.
                StateObject state = new StateObject();

                // Begin receiving the data from the remote device.
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(receiveCallback), state);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                //System.Diagnostics.Debug.WriteLine("receive callback");
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;

                // Read data from the remote device.
                if (socket != null && socket.Connected)
                {

                    int bytesRead = socket.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        if (binaryMode)
                            processReply(state.buffer, bytesRead);
                        else
                        {
                            // There might be more data, so store the data received so far.
                            int co = 0;
                            while (co < bytesRead)
                            {
                                string ch = Encoding.ASCII.GetString(state.buffer, co++, 1);
                                state.sb.Append(ch);
                                if (ch.Equals("\n"))
                                {
                                    //System.Diagnostics.Debug.WriteLine("received: " + state.sb.ToString());
                                    processReply(state.sb.ToString());
                                    state.sb.Clear();
                                }
                            }
                        }
                        receive();
                    } else {
                        _disconnect(false);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private void processReply(string reply)
        {
            //System.Diagnostics.Debug.WriteLine(reply);
            lineReceived?.Invoke(this, new LineReceivedEventArgs { line = reply });
        }

        private void processReply( byte[] bytes, int count )
        {
            bytesReceived?.Invoke(this, new BytesReceivedEventArgs { bytes = bytes, count = count });
        }


        private void send(byte[] byteData)
        {
            if (socket != null && socket.Connected)
            {
                // Begin sending the data to the remote device.
                socket.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(sendCallback), null);
            }
        }

        public void send(string data)
        {
            if (socket != null && socket.Connected)
            {
                //Debug.WriteLine("sending: " + data);
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                send(byteData);
            }
        }

        public void sendBytes( byte[] data)
        {
            send(data);
        }


        public void sendCommand( string cmd )
        {
            send(cmd + lineBreak);
        }

        private void sendCallback(IAsyncResult ar)
        {
            try
            {

                // Complete sending the data to the remote device.
                int bytesSent = socket.EndSend(ar);
                //Debug.WriteLine("Sent {0} bytes to server.", bytesSent);

                
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

    }

    

}

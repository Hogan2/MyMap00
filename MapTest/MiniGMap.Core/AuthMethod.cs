using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniGMap.Core
{
    internal abstract class AuthMethod
    {
        /// <summary>
        /// Initializes an AuthMethod instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        public AuthMethod(Socket server)
        {
            Server = server;
        }
        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <exception cref="ProxyException">Authentication with the proxy server failed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public abstract void Authenticate();
        /// <summary>
        /// Authenticates the user asynchronously.
        /// </summary>
        /// <param name="callback">The method to call when the authentication is complete.</param>
        /// <exception cref="ProxyException">Authentication with the proxy server failed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public abstract void BeginAuthenticate(HandShakeComplete callback);
        /// <summary>
        /// Gets or sets the socket connection with the proxy server.
        /// </summary>
        /// <value>The socket connection with the proxy server.</value>
        protected Socket Server
        {
            get
            {
                return m_Server;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_Server = value;
            }
        }
        /// <summary>
        /// Gets or sets a byt array that can be used to store data.
        /// </summary>
        /// <value>A byte array to store data.</value>
        protected byte[] Buffer
        {
            get
            {
                return m_Buffer;
            }
            set
            {
                m_Buffer = value;
            }
        }
        /// <summary>
        /// Gets or sets the number of bytes that have been received from the remote proxy server.
        /// </summary>
        /// <value>An integer that holds the number of bytes that have been received from the remote proxy server.</value>
        protected int Received
        {
            get
            {
                return m_Received;
            }
            set
            {
                m_Received = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the Buffer property.</summary>
        private byte[] m_Buffer;
        /// <summary>Holds the value of the Server property.</summary>
        private Socket m_Server;
        /// <summary>Holds the address of the method to call when the proxy has authenticated the client.</summary>
        protected HandShakeComplete CallBack;
        /// <summary>Holds the value of the Received property.</summary>
        private int m_Received;
    }
}

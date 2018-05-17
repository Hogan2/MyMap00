﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniGMap.Core
{
    internal class IAsyncProxyResult : IAsyncResult
    {
        /// <summary>Initializes the internal variables of this object</summary>
        /// <param name="stateObject">An object that contains state information for this request.</param>
        internal void Init(object stateObject)
        {
            m_StateObject = stateObject;
            m_Completed = false;
            if (m_WaitHandle != null)
                m_WaitHandle.Reset();

        }
        /// <summary>Initializes the internal variables of this object</summary>
        internal void Reset()
        {
            m_StateObject = null;
            m_Completed = true;
            if (m_WaitHandle != null)
                m_WaitHandle.Set();
        }
        /// <summary>Gets a value that indicates whether the server has completed processing the call. It is illegal for the server to use any client supplied resources outside of the agreed upon sharing semantics after it sets the IsCompleted property to "true". Thus, it is safe for the client to destroy the resources after IsCompleted property returns "true".</summary>
        /// <value>A boolean that indicates whether the server has completed processing the call.</value>
        public bool IsCompleted
        {
            get
            {
                return m_Completed;
            }
        }
        /// <summary>Gets a value that indicates whether the BeginXXXX call has been completed synchronously. If this is detected in the AsyncCallback delegate, it is probable that the thread that called BeginInvoke is the current thread.</summary>
        /// <value>Returns false.</value>
        public bool CompletedSynchronously
        {
            get
            {
                return false;
            }
        }
        /// <summary>Gets an object that was passed as the state parameter of the BeginXXXX method call.</summary>
        /// <value>The object that was passed as the state parameter of the BeginXXXX method call.</value>
        public object AsyncState
        {
            get
            {
                return m_StateObject;
            }
        }
        /// <summary>
        /// The AsyncWaitHandle property returns the WaitHandle that can use to perform a WaitHandle.WaitOne or WaitAny or WaitAll. The object which implements IAsyncResult need not derive from the System.WaitHandle classes directly. The WaitHandle wraps its underlying synchronization primitive and should be signaled after the call is completed. This enables the client to wait for the call to complete instead polling. The Runtime supplies a number of waitable objects that mirror Win32 synchronization primitives e.g. ManualResetEvent, AutoResetEvent and Mutex.
        /// WaitHandle supplies methods that support waiting for such synchronization objects to become signaled with "any" or "all" semantics i.e. WaitHandle.WaitOne, WaitAny and WaitAll. Such methods are context aware to avoid deadlocks. The AsyncWaitHandle can be allocated eagerly or on demand. It is the choice of the IAsyncResult implementer.
        ///</summary>
        /// <value>The WaitHandle associated with this asynchronous result.</value>
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (m_WaitHandle == null)
                    m_WaitHandle = new ManualResetEvent(false);
                return m_WaitHandle;
            }
        }
        // private variables
        /// <summary>Used internally to represent the state of the asynchronous request</summary>
        internal bool m_Completed = true;
        /// <summary>Holds the value of the StateObject property.</summary>
        private object m_StateObject;
        /// <summary>Holds the value of the WaitHandle property.</summary>
        private ManualResetEvent m_WaitHandle;
    }
}

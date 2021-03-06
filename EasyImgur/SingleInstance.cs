﻿// source: http://web.archive.org/web/20080506103924/http://www.flawlesscode.com/post/2008/02/Enforcing-single-instance-with-argument-passing.aspx
// found at: http://stackoverflow.com/questions/917883/c-sharp-how-to-single-instance-application-that-accepts-new-parameters

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace EasyImgur
{
    /// <summary>
    /// Enforces single instance for an application.
    /// </summary>
    public class SingleInstance : IDisposable
    {
        private readonly Boolean ownsMutex;
        private Mutex mutex;
        private Guid identifier = Guid.Empty;

        /// <summary>
        /// Event raised when arguments are received from successive instances.
        /// </summary>
        public event EventHandler<ArgumentsReceivedEventArgs> ArgumentsReceived;

        /// <summary>
        /// Indicates whether this is the first instance of this application.
        /// </summary>
        public bool IsFirstInstance
        {
            get { return this.ownsMutex; }
        }

        /// <summary>
        /// Enforces single instance for an application.
        /// </summary>
        /// <param name="identifier">An identifier unique to this application.</param>
        public SingleInstance(Guid identifier)
        {
            this.identifier = identifier;
            this.mutex = new Mutex(true, this.identifier.ToString(), out this.ownsMutex);
        }

        /// <summary>
        /// Passes the given arguments to the first running instance of the application.
        /// </summary>
        /// <param name="arguments">The arguments to pass.</param>
        /// <returns>Return true if the operation succeded, false otherwise.</returns>
        public bool PassArgumentsToFirstInstance(string[] arguments)
        {
            if (this.IsFirstInstance)
                throw new InvalidOperationException("This is the first instance.");

            try
            {
                using (var client = new NamedPipeClientStream(this.identifier.ToString()))
                using (var writer = new StreamWriter(client))
                {
                    client.Connect(200);

                    foreach (var argument in arguments)
                        writer.WriteLine(argument);
                }
                return true;
            }
            catch (TimeoutException)
            { } //Couldn't connect to server
            catch (IOException)
            { } //Pipe was broken

            return false;
        }

        /// <summary>
        /// Listens for arguments being passed from successive instances of the applicaiton.
        /// </summary>
        public void ListenForArgumentsFromSuccessiveInstances()
        {
            if (!this.IsFirstInstance)
                throw new InvalidOperationException("This is not the first instance.");
            ThreadPool.QueueUserWorkItem(this.ListenForArguments);
        }

        /// <summary>
        /// Listens for arguments on a named pipe. Function is recursive on all paths.
        /// </summary>
        /// <param name="state">State object required by WaitCallback delegate.</param>
        private void ListenForArguments(object state)
        {
            try
            {
                using (var server = new NamedPipeServerStream(this.identifier.ToString()))
                using (var reader = new StreamReader(server))
                {
                    server.WaitForConnection();

                    var arguments = new List<String>();
                    while (server.IsConnected)
                        arguments.Add(reader.ReadLine());

                    ThreadPool.QueueUserWorkItem(this.CallOnArgumentsReceived, arguments.ToArray());
                }
            }
            catch (IOException)
            { } //Pipe was broken
            finally
            {
                this.ListenForArguments(null);
            }
        }

        /// <summary>
        /// Calls the OnArgumentsReceived method casting the state Object to String[].
        /// </summary>
        /// <param name="state">The arguments to pass.</param>
        private void CallOnArgumentsReceived(object state)
        {
            this.OnArgumentsReceived((string[])state);
        }

        /// <summary>
        /// Fires the ArgumentsReceived event.
        /// </summary>
        /// <param name="arguments">The arguments to pass with the ArgumentsReceivedEventArgs.</param>
        private void OnArgumentsReceived(string[] arguments)
        {
            if (this.ArgumentsReceived != null)
                this.ArgumentsReceived(this, new ArgumentsReceivedEventArgs() { Args = arguments });
        }

        #region IDisposable
        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;
            if (this.mutex != null && this.ownsMutex)
            {
                this.mutex.ReleaseMutex();
                this.mutex = null;
            }
            this.disposed = true;
        }

        ~SingleInstance()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
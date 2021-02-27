using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace EventBusRabbitMQ
{
    class RabbitMQConnection : IRabbitMQConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private bool _disposed;

        public RabbitMQConnection(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            if (this.IsConnected == false)
            {
                this.TryConnect();
            }
        }

        public bool IsConnected
        {
            get
            {
                return (_connection != null 
                            && _connection.IsOpen 
                            && _disposed == false);
            }
        }

        public IModel CreateModel()
        {
            if (this.IsConnected == false)
            {
                throw new InvalidOperationException("Failed to establish RabbitMQ Connection.");
            }
            return _connection.CreateModel();
            
        }

        public void Dispose()
        {
            try
            {
                _connection.Dispose();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool TryConnect()
        {
            try
            {
                _connection = _connectionFactory.CreateConnection();
            }
            catch (BrokerUnreachableException)
            {
                Thread.Sleep(2000);
                _connection = _connectionFactory.CreateConnection();
            }
            return this.IsConnected;
        }
    }
}

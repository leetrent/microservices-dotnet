using EventBusRabbitMQ.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBusRabbitMQ.Producer
{
   public class EventBusRabbitMQProducer
    {
        private readonly IRabbitMQConnection _connection;

        public EventBusRabbitMQProducer(IRabbitMQConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public void PublishBasketCheckout(string queueName, BasketCheckoutEvent publishModel)
        {
            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare
                (
                    queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                string serializedMessage = JsonConvert.SerializeObject(publishModel);

                Console.WriteLine("[EventBusRabbitMQProducer][PublishBasketCheckout] => (serializedMessage): ");
                Console.WriteLine(serializedMessage);
                Console.WriteLine("");

                byte[] messageByteArray = Encoding.UTF8.GetBytes(serializedMessage);

                IBasicProperties basicProps = channel.CreateBasicProperties();
                basicProps.Persistent = true;   
                basicProps.DeliveryMode = 2;

                channel.ConfirmSelect();
                channel.BasicPublish
                (
                    exchange: "",
                    routingKey: queueName,
                    mandatory: true,
                    basicProperties: basicProps,
                    body: messageByteArray
                );
                channel.WaitForConfirmsOrDie();

                channel.BasicAcks += (sender, eventArgs) =>
                {
                    Console.WriteLine("[EventBusRabbitMQProducer][PublishBasketCheckout] => (IN 'channel.BasicAcks')");
                };
                channel.ConfirmSelect();
            }
        }
    }
}

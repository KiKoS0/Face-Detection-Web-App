using Microsoft.AspNetCore.Hosting;
using RabbitMQ.Client;
using FaceDetectionWeb.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceDetectionWeb.RabbitMQ
{
    public class RabbitMQClient
    {
        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static IModel _model;

        private readonly IHostingEnvironment _environment;

        private const string ExchangeName = "Image_Exchange";
        private const string ImageQueueName = "Images_Queue";
        private const string AllQueueName = "AllTopic_Queue";

        public RabbitMQClient(IHostingEnvironment environment)
        {
            _environment = environment;
            CreateConnection();
        }

        private static void CreateConnection()
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = _factory.CreateConnection();
            _model = _connection.CreateModel();
            _model.ExchangeDeclare(ExchangeName, "topic");

            _model.QueueDeclare(ImageQueueName, true, false, false, null);
            //_model.QueueDeclare(AllQueueName, true, false, false, null);

            _model.QueueBind(ImageQueueName, ExchangeName, "image.path");

            //_model.QueueBind(AllQueueName, ExchangeName, "image.*");
        }


        public void Close()
        {
            _connection.Close();
        }

        public void SendMessage(byte[] message, string routingKey)
        {
            _model.BasicPublish(ExchangeName, routingKey, null, message);
        }

        public void SendImagePath(Image image)
        {
            SendMessage(Encoding.UTF8.GetBytes(Path.Combine(_environment.WebRootPath, image.ImageUrl)), "image.path");
            Console.WriteLine(" Image path Sent {0}", image.ImageUrl);
        }

    }
}

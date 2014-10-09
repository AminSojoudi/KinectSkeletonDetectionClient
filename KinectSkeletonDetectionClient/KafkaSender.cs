using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSkeletonDetectionClient
{
    class KafkaSender
    {
        private const string KAFKA_TOPIC = "test";
        private const string KAFKA_SERVER_ADDRESS = "http://10.211.55.4:9092";
        private static byte[] message;

        private KafkaOptions options;
        private BrokerRouter router;
        private Producer client;
        private bool isConnected;


        public KafkaSender()
        {
            try
            {
                options = new KafkaOptions(new Uri(KAFKA_SERVER_ADDRESS));
                router = new BrokerRouter(options);
                client = new Producer(router);
                isConnected = true;
            }
            catch(Exception e)
            {
                isConnected = false;
            }
        }

        public bool getStatus()
        {
            return isConnected;
        }

        public void sendMessage(string _message)
        {
            message = Encoding.UTF8.GetBytes(_message);
            try
            {
                client.SendMessageAsync(KAFKA_TOPIC, new[] { new Message { Value = message } }).Wait();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

using StackExchange.Redis;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheRedis.Service
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase db;
        public RedisService(string ConnectionString)
        {
            _redis = ConnectionMultiplexer.Connect(ConnectionString);
            db = _redis.GetDatabase();
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await db.KeyDeleteAsync(key);
        }
        void PublishMessage<T>(string queueName, T message)
        {
            IDatabase db = _redis.GetDatabase();
            string serializedMessage = JsonConvert.SerializeObject(message);
            db.ListLeftPush(queueName, serializedMessage);
        }
        public T DequeueMessage<T>(string queueName)
        {
            RedisValue serializedMessage = db.ListLeftPop(queueName);
            if (!serializedMessage.IsNull)
            {
                return JsonConvert.DeserializeObject<T>(serializedMessage);
            }
            return default(T);
        }
        public void Dispose()
        {
            _redis.Dispose();
        }

        public async Task<T> GetObjectAsync<T>(string key)
        {
            string serializedValue = await db.StringGetAsync(key);
            if (!string.IsNullOrEmpty(serializedValue))
            {
                return JsonConvert.DeserializeObject<T>(serializedValue);
            }
            else
            {
                return default(T);
            }
        }

        public async Task<RedisKey[]> GetObjectAllAsync(string key)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = new List<RedisKey>();

            await foreach (var redisKey in server.KeysAsync(pattern: key ))
            {
                keys.Add(redisKey);
            }

            return keys.ToArray();
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value)
        {
            string serializedValue = JsonConvert.SerializeObject(value);
            return await db.StringSetAsync(key, serializedValue);

        }

        public async Task<bool> UpdateObjectAsync<T>(string key, T value)
        {
            if (await db.KeyExistsAsync(key))
            {
                string serializedValue = JsonConvert.SerializeObject(value);
                return await db.StringSetAsync(key, serializedValue);
            }
            else
            {
                throw new InvalidOperationException("Key does not exist");
            }
        }
        public bool HasNextMessage(string queueName)
        {
            return db.ListLength(queueName) > 0;
        }
    }

}

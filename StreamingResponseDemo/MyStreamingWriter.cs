using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamingResponseDemo
{
    public class MyStreamingWriter
    {
        private const int DefaultBufferSize = 0x10000;
        private const int MinBufferSize = 0x1000;
        private const int MaxFieldLength = 0x100;

        private const byte StartOfField = 0x01; //SOH
        private const byte EndOfContent = 0x03; //ETX

        public MyStreamingWriter(
            HttpContext context,
            int bufferSize = DefaultBufferSize
            )
        {
            if (bufferSize < MinBufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), $"Min buffer size is {MinBufferSize} bytes.");
            }
            this._stream = context.Response.Body;
            this._buffer = new byte[bufferSize];

            context.Response.StatusCode = 200;   //OK
            context.Response.Headers.Add(HeaderNames.ContentType, "text/plain");
        }

        private readonly Stream _stream;
        private readonly byte[] _buffer;

        public async Task BeginFieldAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            else if (name.Length > MaxFieldLength)
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"Max field length is {MaxFieldLength} chars.");
            }
            this._buffer[0] = StartOfField;
            int byteCount = Encoding.UTF8.GetBytes(name, 0, name.Length, this._buffer, 1);
            this._buffer[byteCount + 1] = EndOfContent;
            await this._stream.WriteAsync(this._buffer, 0, byteCount + 2);
        }

        public async Task WriteAsync(object content)
        {
#if NETSTANDARD2_0 || NETCOREAPP2_2
            string s = Newtonsoft.Json.JsonConvert.SerializeObject(content, Newtonsoft.Json.Formatting.None);
#else
            string s = System.Text.Json.JsonSerializer.Serialize(content);
#endif
            int byteCount = Encoding.UTF8.GetBytes(s, 0, s.Length, this._buffer, 0);
            this._buffer[byteCount] = EndOfContent;
            await this._stream.WriteAsync(this._buffer, 0, byteCount + 1);
        }

        public async Task EndAsync()
        {
            await this._stream.FlushAsync();
        }
    }
}

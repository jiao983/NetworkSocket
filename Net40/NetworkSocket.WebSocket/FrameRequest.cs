﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSocket.WebSocket
{
    /// <summary>
    /// 表示请求帧
    /// </summary>
    public class FrameRequest 
    {
        /// <summary>
        /// 获取是否已完成
        /// </summary>
        public bool Fin { get; private set; }

        /// <summary>
        /// 获取保存位
        /// </summary>
        public ByteBits Rsv { get; private set; }

        /// <summary>
        /// 获取帧类型
        /// </summary>
        public FrameCodes Frame { get; private set; }

        /// <summary>
        /// 获取是否有掩码
        /// </summary>
        public bool Mask { get; private set; }

        /// <summary>
        /// 获取内容长度
        /// </summary>
        public int ContentLength { get; private set; }

        /// <summary>
        /// 获取掩码
        /// </summary>
        public byte[] MaskingKey { get; private set; }

        /// <summary>
        /// 获取请求帧的内容
        /// </summary>
        public byte[] Content { get; private set; }


        /// <summary>
        /// 解析请求的数据
        /// 返回请求数据包
        /// </summary>
        /// <param name="builder">所有收到的数据</param>  
        /// <returns></returns>
        public unsafe static FrameRequest From(ByteBuilder builder)
        {
            if (builder.Length < 2)
            {
                return null;
            }

            ByteBits byte0 = builder[0];
            var fin = byte0[0];
            var frameCode = (FrameCodes)(byte)byte0.Take(4, 4);

            if (fin == false || frameCode == FrameCodes.Continuation)
            {
                return null;
            }

            var rsv = byte0.Take(1, 3);
            ByteBits byte1 = builder[1];
            var mask = byte1[0];

            if (mask == false || Enum.IsDefined(typeof(FrameCodes), frameCode) == false || rsv != 0)
            {
                return null;
            }

            var contentLength = (int)byte1.Take(1, 7);
            builder.Position = 2;

            if (contentLength == 127)
            {
                contentLength = (int)builder.ReadUInt64();
            }
            else if (contentLength == 126)
            {
                contentLength = (int)builder.ReadUInt16();
            }

            var maskingKey = builder.ReadArray(4);
            var packetLength = builder.Position + contentLength;

            if (builder.Length < packetLength)
            {
                return null;
            }

            var content = builder.ReadArray(contentLength);
            builder.Clear(packetLength);

            fixed (byte* pcontent = &content[0], pmask = &maskingKey[0])
            {
                for (var i = 0; i < contentLength; i++)
                {
                    *(pcontent + i) = (byte)(*(pcontent + i) ^ *(pmask + i % 4));
                }
            }

            return new FrameRequest
            {
                Fin = fin,
                Rsv = rsv,
                Mask = mask,
                Frame = frameCode,
                ContentLength = contentLength,
                MaskingKey = maskingKey,
                Content = content
            };
        }
    }
}

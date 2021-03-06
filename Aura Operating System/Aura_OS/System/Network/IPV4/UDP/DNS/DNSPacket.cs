﻿/*
* PROJECT:          Aura Operating System Development
* CONTENT:          DNS Packet
* PROGRAMMERS:      Valentin Charbonnier <valentinbreiz@gmail.com>
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Aura_OS.System.Network.IPV4.UDP.DNS
{

    /// <summary>
    ///  ReplyCode set in Flags
    /// </summary>
    public enum ReplyCode
    {
        OK = 0000,
        FormatError = 0001,
        ServerFailure = 0010,
        NameError = 0011,
        NotSupported = 0100,
        Refused = 0101
    }

    /// <summary>
    /// DNS Query
    /// </summary>
    public class DNSQuery
    {
        public string Name { get; set; }
        public ushort Type { get; set; }
        public ushort Class { get; set; }
    }

/// <summary>
    /// DNS Answer
    /// </summary>
    public class DNSAnswer
    {
        public ushort Name { get; set; }
        public ushort Type { get; set; }
        public ushort Class { get; set; }
        public int TimeToLive { get; set; }
        public ushort DataLenght { get; set; }
        public byte[] Address { get; set; }
    }

    public class DNSPacket : UDPPacket
    {
        protected ushort transactionID;
        protected ushort dNSFlags;
        protected ushort questions;
        protected ushort answerRRs;
        protected ushort authorityRRs;
        protected ushort additionalRRs;
        protected List<DNSQuery> queries;
        protected List<DNSAnswer> answers;

        internal static void DNSHandler(byte[] packetData)
        {
            DNSPacket dns_packet = new DNSPacket(packetData);

            DnsClient receiver = (DnsClient)UdpClient.Client(dns_packet.DestinationPort);
            if (receiver != null)
            {
                receiver.receiveData(dns_packet);
            }
        }

        /// <summary>
        /// Work around to make VMT scanner include the initFields method
        /// </summary>
        public static void VMTInclude()
        {
            new DNSPacket();
        }

        internal DNSPacket()
            : base()
        { }

        public DNSPacket(byte[] rawData)
            : base(rawData)
        { }

        public DNSPacket(Address source, Address dest, ushort urlnb, ushort len)
            : base(source, dest, 53, 53, (ushort)(len + 12))
        {
            Random rnd = new Random();
            byte transactionID = (byte)rnd.Next(0, Int32.MaxValue);
            mRawData[this.dataOffset + 8] = (byte)((transactionID >> 8) & 0xFF);
            mRawData[this.dataOffset + 9] = (byte)((transactionID >> 0) & 0xFF);

            mRawData[this.dataOffset + 10] = (byte)((0x0100 >> 8) & 0xFF);
            mRawData[this.dataOffset + 11] = (byte)((0x0100 >> 0) & 0xFF);

            mRawData[this.dataOffset + 12] = (byte)((urlnb >> 8) & 0xFF);
            mRawData[this.dataOffset + 13] = (byte)((urlnb >> 0) & 0xFF);

            mRawData[this.dataOffset + 14] = (byte)((0 >> 8) & 0xFF);
            mRawData[this.dataOffset + 15] = (byte)((0 >> 0) & 0xFF);

            mRawData[this.dataOffset + 16] = (byte)((0 >> 8) & 0xFF);
            mRawData[this.dataOffset + 17] = (byte)((0 >> 0) & 0xFF);

            mRawData[this.dataOffset + 18] = (byte)((0 >> 8) & 0xFF);
            mRawData[this.dataOffset + 19] = (byte)((0 >> 0) & 0xFF);

            initFields();
        }

        protected override void initFields()
        {
            base.initFields();
            transactionID = (UInt16)((mRawData[this.dataOffset + 8] << 8) | mRawData[this.dataOffset + 9]);
            dNSFlags = (UInt16)((mRawData[this.dataOffset + 10] << 8) | mRawData[this.dataOffset + 11]);
            questions = (UInt16)((mRawData[this.dataOffset + 12] << 8) | mRawData[this.dataOffset + 13]);
            answerRRs = (UInt16)((mRawData[this.dataOffset + 14] << 8) | mRawData[this.dataOffset + 15]);
            authorityRRs = (UInt16)((mRawData[this.dataOffset + 16] << 8) | mRawData[this.dataOffset + 17]);
            additionalRRs = (UInt16)((mRawData[this.dataOffset + 18] << 8) | mRawData[this.dataOffset + 19]);
        }

        public string parseName(byte[] mRawData, ref int index)
        {
            StringBuilder url = new StringBuilder();

            while (mRawData[index] != 0x00 && index < mRawData.Length)
            {
                byte wordlength = mRawData[index];
                index++;
                for (int j = 0; j < wordlength; j++)
                {
                    url.Append((char)mRawData[index]);
                    index++;
                }
                url.Append('.');
            }
            index++; //End 0x00
            return (url.ToString().Remove(url.Length - 1, 1));
        }

        internal ushort TransactionID
        {
            get { return this.transactionID; }
        }

        internal ushort DNSFlags
        {
            get { return this.dNSFlags; }
        }

        internal ushort Questions
        {
            get { return this.questions; }
        }

        internal List<DNSQuery> Queries
        {
            get { return this.queries;  }
        }

        internal List<DNSAnswer> Answers
        {
            get { return this.answers; }
        }

        public override string ToString()
        {
            return "DNS Packet Src=" + sourceIP + ":" + SourcePort + ", Dest=" + destIP + ":" + DestinationPort;
        }

    }
    public class DNSPacketAsk : DNSPacket
    {
        /// <summary>
        /// Work around to make VMT scanner include the initFields method
        /// </summary>
        public static void VMTInclude()
        {
            new DNSPacketAsk();
        }

        internal DNSPacketAsk()
            : base()
        { }

        public DNSPacketAsk(byte[] rawData)
            : base(rawData)
        { }

        public DNSPacketAsk(Address source, Address dest, string url)
            : base(source, dest, 1, (ushort)(5 + url.Length + 1))
        {
            int b = 0;

            foreach (string item in url.Split('.'))
            {
                byte[] word = Encoding.ASCII.GetBytes(item);

                mRawData[this.dataOffset + 20 + b] = (byte)word.Length; //set word length

                b++;

                foreach (byte letter in word)
                {
                    mRawData[this.dataOffset + 20 + b] = letter;
                    b++;
                }

            }

            mRawData[this.dataOffset + 20 + b] = 0x00;

            mRawData[this.dataOffset + 20 + b + 1] = 0x00;
            mRawData[this.dataOffset + 20 + b + 2] = 0x01;

            mRawData[this.dataOffset + 20 + b + 3] = 0x00;
            mRawData[this.dataOffset + 20 + b + 4] = 0x01;
        }

        protected override void initFields()
        {
            base.initFields();
        }
    }

    public class DNSPacketAnswer : DNSPacket
    {
        /// <summary>
        /// Work around to make VMT scanner include the initFields method
        /// </summary>
        public static void VMTInclude()
        {
            new DNSPacketAnswer();
        }

        internal DNSPacketAnswer()
            : base()
        { }

        public DNSPacketAnswer(byte[] rawData)
            : base(rawData)
        { }

        protected override void initFields()
        {
            base.initFields();

            if ((ushort)(DNSFlags & 0x0F) != (ushort)ReplyCode.OK)
            {
                Console.WriteLine("DNS Packet response not OK. Passing packet.");
                return;
            }

            int index = dataOffset + 20;
            if (questions > 0)
            {
                queries = new List<DNSQuery>();

                for (int i = 0; i < questions; i++)
                {
                    DNSQuery query = new DNSQuery();
                    query.Name = parseName(mRawData, ref index);
                    query.Type = (ushort)((mRawData[index + 0] << 8) | mRawData[index + 1]);
                    query.Class = (ushort)((mRawData[index + 2] << 8) | mRawData[index + 3]);
                    queries.Add(query);
                    index += 4;
                }
            }
            if (answerRRs > 0)
            {
                answers = new List<DNSAnswer>();

                for (int i = 0; i < answerRRs; i++)
                {
                    DNSAnswer answer = new DNSAnswer();
                    answer.Name = (ushort)((mRawData[index + 0] << 8) | mRawData[index + 1]);
                    answer.Type = (ushort)((mRawData[index + 2] << 8) | mRawData[index + 3]);
                    answer.Class = (ushort)((mRawData[index + 4] << 8) | mRawData[index + 5]);
                    answer.TimeToLive = (mRawData[index + 6] << 24) | (mRawData[index + 7] << 16) | (mRawData[index + 8] << 8) | mRawData[index + 9];
                    answer.DataLenght = (ushort)((mRawData[index + 10] << 8) | mRawData[index + 11]);
                    index += 12;
                    answer.Address = new byte[answer.DataLenght];
                    for (int j = 0; j < answer.DataLenght; j++, index++)
                    {
                        answer.Address[j] = mRawData[index];
                    }
                    answers.Add(answer);
                }
            }
        }

    }
}
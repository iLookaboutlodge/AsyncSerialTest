using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncSerialTest2
{
    public class StringReader
    {
        private string startDelimiter;
        private string endDelimiter;

        private Stream baseStream;
        private bool endOfStream;

        private byte[] currentBuffer;
        private byte[] startDelimiterBytes;
        private byte[] endDelimiterBytes;
        private int minPacketLength;
        private bool includeStartDelimiter;
        private bool includeEndDelimiter;

        public StringReader(Stream baseStream, string startDelimiter, string endDelimiter, int bufferSize = 2048, bool includeStartDelimiter = true, bool includeEndDelimiter = false)
        {
            if (startDelimiter == null) startDelimiter = "";
            if (startDelimiter == "") includeStartDelimiter = true;
            this.baseStream = baseStream;
            this.startDelimiter = startDelimiter;
            this.endDelimiter = endDelimiter;
            this.includeStartDelimiter = includeStartDelimiter;
            this.includeEndDelimiter = includeEndDelimiter;
            currentBuffer = new byte[bufferSize];

            startDelimiterBytes = Encoding.ASCII.GetBytes(startDelimiter);
            endDelimiterBytes = Encoding.ASCII.GetBytes(endDelimiter);
            minPacketLength = startDelimiterBytes.Length + endDelimiterBytes.Length;
        }

        public bool EndOfStream { get { return endOfStream; } }


        public string Read()
        {
            string msg = null;
            int currentPtr = 0;
            bool inMsg = (startDelimiter == ""); //Always inMsg if no start delimiter, otherwise assume not inMsg 

            while (msg == null)
            {
                int b = baseStream.ReadByte();
                if (b < 0)
                {
                    endOfStream = true;
                    break;
                }

                //Make sure the start of the packet matches the startDelimiter
                if (currentPtr < startDelimiterBytes.Length)
                {
                    inMsg = (b == startDelimiterBytes[currentPtr]);
                    if (!inMsg) currentPtr = 0;
                }

                if (inMsg)
                {
                    currentBuffer[currentPtr++] = (byte)b;
                    if (currentPtr == currentBuffer.Length) throw new InternalBufferOverflowException("The packet length exceeds the current buffer size.");

                    if (currentPtr >= minPacketLength)
                    {
                        int i = endDelimiterBytes.Length - 1;
                        if (b == endDelimiterBytes[i])
                        {
                            //Possible end of packet... see if all of the previous bytes match the endDelimiter
                            bool done = true;
                            
                            while (i > 0)
                            {
                                i--;
                                if ((currentBuffer[currentPtr - endDelimiterBytes.Length + i] != endDelimiterBytes[i]))
                                {
                                    done = false;
                                    break;
                                }
                            }
                            if (done)
                            {
                                //Yes, all bytes matched... end of packet reached
                                int start;
                                if (includeStartDelimiter) start = 0; else start = startDelimiter.Length;
                                int end;
                                if (includeEndDelimiter) end = currentPtr; else end = currentPtr - endDelimiter.Length;
                                msg = Encoding.ASCII.GetString(currentBuffer, start, end - start);
                            }
                        }
                    }
                }
            }
            return msg;
        }
    }


}


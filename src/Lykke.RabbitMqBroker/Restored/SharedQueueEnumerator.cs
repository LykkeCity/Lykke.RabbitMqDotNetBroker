using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Lykke.RabbitMqBroker.Restored
{
    public struct SharedQueueEnumerator<T> : IEnumerator<T>
    {
        private readonly SharedQueue<T> m_queue;
        private T m_current;

        ///<summary>Construct an enumerator for the given
        ///SharedQueue.</summary>
        public SharedQueueEnumerator(SharedQueue<T> queue)
        {
            m_queue = queue;
            m_current = default(T);
        }

        object IEnumerator.Current
        {
            get
            {
                if (m_current == null)
                {
                    throw new InvalidOperationException();
                }
                return m_current;
            }
        }

        T IEnumerator<T>.Current
        {
            get
            {
                if (m_current == null)
                {
                    throw new InvalidOperationException();
                }
                return m_current;
            }
        }

        public void Dispose()
        {
        }

        bool IEnumerator.MoveNext()
        {
            try
            {
                m_current = m_queue.Dequeue();
                return true;
            }
            catch (EndOfStreamException)
            {
                m_current = default(T);
                return false;
            }
        }

        ///<summary>Reset()ting a SharedQueue doesn't make sense, so
        ///this method always throws
        ///InvalidOperationException.</summary>
        void IEnumerator.Reset()
        {
            throw new InvalidOperationException("SharedQueue.Reset() does not make sense");
        }
    }
}

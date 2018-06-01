﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.FacialRemote
{
    [Serializable]
    public class PlaybackBuffer
    {
        [SerializeField]
        string m_Name;

        [SerializeField]
        [HideInInspector]
        byte[] m_RecordStream = { };

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public byte[] recordStream { get { return m_RecordStream; } set { m_RecordStream = value; } }

        Queue<byte[]> m_RecordQueue;

        public Queue<byte[]> recordQueue
        {
            get
            {
                if (m_RecordQueue == null)
                {
                    m_RecordQueue = QueueRecordStream(recordStream, 266);
                }
                return m_RecordQueue;
            }
        }

        static Queue<byte[]> QueueRecordStream(byte[] stream, int bufferSize = 266)
        {
            var queue = new Queue<byte[]>();
            var empty = new byte();
            for (var i = 0; i < stream.Length;)
            {
                var bytes = new byte[bufferSize];
                for (var b = 0; b < bytes.Length; b++)
                {
                    bytes[b] = i + b < stream.Length ? stream[i + b] : empty;
                }

                queue.Enqueue(bytes);
                i += bufferSize;
            }
            return queue;
        }
    }

    [Serializable]
    [CreateAssetMenu(fileName = "PlaybackData", menuName = "FacialRemote/PlaybackData")]
    public class PlaybackData : ScriptableObject
    {
        [SerializeField]
        PlaybackBuffer[] m_PlaybackBuffers;

        public PlaybackBuffer[] playbackBuffers { get { return m_PlaybackBuffers; } }

        PlaybackBuffer m_ActiveBuffer;
        public List<byte[]> activeByteRecord { get; private set; }

        [SerializeField]
        [HideInInspector]
        byte[] m_LastRecord;

        void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += EditorStateChange;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= EditorStateChange;
#endif
        }

#if UNITY_EDITOR
        void EditorStateChange(PlayModeStateChange state)
        {
            byte errorCheck = 42;
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (m_ActiveBuffer != null)
                {
                    m_LastRecord = new byte[activeByteRecord.Count * 266];
                    var buffer = new byte[266];
                    for (var i = 0; i < activeByteRecord.Count; i++)
                    {
                        buffer = activeByteRecord[i];
                        Buffer.BlockCopy(buffer, 0, m_LastRecord, i * 266, 266);
                        if (buffer[0] != errorCheck)
                            Debug.LogError(string.Format("Error in buffer {0}", i));
                    }
                      //  activeByteQueue.SelectMany(s => s).ToArray();
                    m_ActiveBuffer.recordStream = m_LastRecord.ToArray();
                    EditorUtility.SetDirty(this);

                    if (m_ActiveBuffer.recordStream.Length < 1 && m_PlaybackBuffers.Contains(m_ActiveBuffer))
                    {
                        m_PlaybackBuffers = m_PlaybackBuffers.Where(s => s != m_ActiveBuffer).ToArray();
                    }

                    m_ActiveBuffer = null;
                    activeByteRecord.Clear();
                }
            }
        }
#endif

        public void CreatePlaybackBuffer()
        {
            var buffers = m_PlaybackBuffers.ToList();
            var buffer = new PlaybackBuffer();
            buffer.name = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            buffers.Add(buffer);
            m_PlaybackBuffers = buffers.ToArray();
            m_ActiveBuffer = m_PlaybackBuffers[m_PlaybackBuffers.Length-1];
            activeByteRecord = new List<byte[]>();
        }
    }
}

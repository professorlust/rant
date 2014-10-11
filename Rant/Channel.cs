﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Rant
{
    /// <summary>
    /// Stores output from a pattern channel.
    /// </summary>
    public sealed class Channel
    {
        internal const int InitialBufferSize = 512;

        private StringBuilder _currentBuffer;
        private readonly List<StringBuilder> _buffers;
        private readonly Dictionary<string, StringBuilder> _backPrintPoints = new Dictionary<string, StringBuilder>();
        private readonly Dictionary<string, StringBuilder> _forePrintPoints = new Dictionary<string, StringBuilder>();
        private readonly Dictionary<StringBuilder, Tuple<StringBuilder, Capitalization, char>> _articleConverters = new Dictionary<StringBuilder, Tuple<StringBuilder, Capitalization, char>>();

        private int _length = 0;
        private int _bufferCount = 0;

        private char _lastChar = ' ';
        private Capitalization _caps = Capitalization.None;

        /// <summary>
        /// The name of the channel.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The visibility of the channel.
        /// </summary>
        public ChannelVisibility Visiblity { get; internal set; }

        internal Capitalization Capitalization
        {
            get { return _caps; }
            set { _caps = value; }
        }

        internal Channel(string name, ChannelVisibility visibility)
        {
            Name = name;
            Visiblity = visibility;
            _currentBuffer = new StringBuilder(InitialBufferSize);
            _buffers = new List<StringBuilder>{_currentBuffer};
        }

        internal void Write(string value)
        {
            _length += value.Length;
            _currentBuffer.Append(Util.Capitalize(value, ref _caps, ref _lastChar));
            UpdateArticle(_currentBuffer);
        }

        internal void WriteArticle()
        {
            char lc = _lastChar;
            var caps = _caps;
            var anBuilder = Tuple.Create(new StringBuilder().Append(Util.Capitalize(Engine.CurrentIndefiniteArticleI.ConsonantForm, ref _caps, ref _lastChar)), caps, lc);
            var afterBuilder = _currentBuffer = new StringBuilder();
            _articleConverters[afterBuilder] = anBuilder;
            _buffers.Add(anBuilder.Item1);
            _buffers.Add(_currentBuffer = afterBuilder);
            _bufferCount += 2;
        }

        private void UpdateArticle(StringBuilder target)
        {
            Tuple<StringBuilder, Capitalization, char> aBuilder;
            if (!_articleConverters.TryGetValue(target, out aBuilder)) return;
            if (target.Length == 0) // Clear to "a" if the after-buffer is empty
            {
                int l1 = aBuilder.Item1.Length;
                aBuilder.Item1.Clear().Append(Util.Capitalize(Engine.CurrentIndefiniteArticleI.ConsonantForm, aBuilder.Item2, aBuilder.Item3));
                _length += -l1 + aBuilder.Item1.Length;
                return;
            }

            if (Engine.CurrentIndefiniteArticleI.CanPluralize(target))
            {
                int l1 = aBuilder.Item1.Length;
                aBuilder.Item1.Clear().Append(Util.Capitalize(Engine.CurrentIndefiniteArticleI.Vowel, aBuilder.Item2, aBuilder.Item3));
                _length += -l1 + aBuilder.Item1.Length;
            }
        }

        internal void WriteToPoint(string name, string value)
        {
            StringBuilder sb;
            if (!_backPrintPoints.TryGetValue(name, out sb))
            {
                _forePrintPoints[name] = new StringBuilder(InitialBufferSize).Append(Util.Capitalize(value, ref _caps, ref _lastChar));
            }
            else
            {
                sb.Append(Util.Capitalize(value, ref _caps, ref _lastChar));
                UpdateArticle(sb);
            }
        }

        internal void CreateNamedWritePoint(string name)
        {
            StringBuilder sp;
            if (_forePrintPoints.TryGetValue(name, out sp))
            {
                _buffers.Add(sp);
                _bufferCount++;
                _forePrintPoints.Remove(name);
            }
            else
            {
                StringBuilder sb;
                if (!_backPrintPoints.TryGetValue(name, out sb))
                {
                    sb = _backPrintPoints[name] = new StringBuilder(InitialBufferSize);
                }
                _buffers.Add(sb);
                _bufferCount++;
            }

            _buffers.Add(_currentBuffer = new StringBuilder(InitialBufferSize));
            _bufferCount++;
        }

        internal int MeasureDistance(int bufIndexA, int bufIndexB, int bufCharA, int bufCharB)
        {
            int ia = Math.Min(bufIndexA, bufIndexB);
            int ib = Math.Max(bufIndexA, bufIndexB);
            int len = bufCharB;
            for (int i = ia; i < ib; i++)
            {
                len += _buffers[i].Length;
            }
            return len - bufCharA;
        }

        internal int CurrentBufferIndex
        {
            get { return _bufferCount; }
        }

        internal int CurrentBufferLength
        {
            get { return _currentBuffer.Length; }
        }

        /// <summary>
        /// The number of characters in the output.
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// The output string stored in the channel.
        /// </summary>
        public string Value
        {
            get
            {
                var sb = new StringBuilder(InitialBufferSize);
                foreach (var b in _buffers)
                {
                    sb.Append(b);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns a string that identifies the channel by name and visibility.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Concat(Name, " (", Visiblity, ")");
        }
    }
}
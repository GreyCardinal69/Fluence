using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testing_Chamber.Minis;

namespace Fluence
{
    internal class FluenceLexer
    {
        private readonly string _sourceCode;
        private readonly int _sourceLength;
        private int _currentPosition;
        private int _currentLine;
        private Queue<Token> _tokenQueue;

        internal int CurrentLine => _currentLine;
        internal bool HasReachedEnd => _currentPosition >= _sourceLength && _tokenQueue.Count == 0;

        internal FluenceLexer( string source )
        {
            _sourceCode = source;
            _sourceLength = source.Length;
            _currentPosition = 0;
            _currentLine = 0;
            _tokenQueue = new Queue<Token>();
        }

        internal Token GetNextToken()
        {

        }
    }
}
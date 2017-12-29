using System.Collections;
using System.Collections.Generic;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// CommandArgumentEnumerator Class.
    /// </summary>
    public class CommandArgumentEnumerator : IEnumerator<CommandArgument>
    {
        private readonly IEnumerator<CommandArgument> _enumerator;
        
        public CommandArgumentEnumerator(IEnumerator<CommandArgument> enumerator)
        {
            _enumerator = enumerator;
        }

        public CommandArgument Current
        {
            get => _enumerator.Current;
        }

        object IEnumerator.Current
        {
            get => Current;
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext()
        {
            if (Current == null || !Current.MultipleValues)
            {
                return _enumerator.MoveNext();
            }

            // If current argument allows multiple values, we don't move forward and
            // all later values will be added to current CommandArgument.Values
             return true;
        }

        public void Reset()
        {
            _enumerator.Reset();
        }
    }
}
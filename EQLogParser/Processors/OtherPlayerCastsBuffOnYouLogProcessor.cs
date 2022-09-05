using System.Collections.Generic;
using System.Linq;

namespace EQLogParser.Processors
{
    public class OtherPlayerCastsBuffOnYouLogProcessor : ILogProcessor
    {
        public EverquestLogReader.LogType LogType => EverquestLogReader.LogType.OtherPlayerCastsBuffOnYou;

        private readonly SpellCache _spellCache;
        private readonly IBuffManager _buffManager;
        private IEnumerable<Spell> _spells;
        public OtherPlayerCastsBuffOnYouLogProcessor(SpellCache spellCache, IBuffManager buffManager)
        {
            _spellCache = spellCache;
            _buffManager = buffManager;
        }

        public bool IsMatch(LogLine line)
        {

            IEnumerable<Spell> spells = _spellCache.GetSpellsByMessage(line.Message).ToList();
            if (_spells!=null && _spells.Any())
            {
                if (_spells.Count(x => x.MessageYou == line.Message && x.TargetType == TargetTypes.Single) == 1)
                {

                    _spells = spells;
                    return true;
                }
                else
                {
                    //can't match spell
                }
            }

            return false;
        }

        public void Process(LogLine line)
        {
            var v = this._spells.Where(x => x.MessageYou == line.Message && x.TargetType == TargetTypes.Single);
            Spell spell = this._spells.SingleOrDefault(x => x.MessageYou == line.Message && x.TargetType == TargetTypes.Single);

            if (spell != null)
            {
                _buffManager.AddBuff("__YOU__", spell.ToBuff(line.When));
            }
        }
    }
}
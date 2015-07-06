using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar
{
    /// <summary>
    /// Class describing a predicate (boolean) equation. This class has no knowledge of protocol structure,
    /// and stores identifiers as strings. The predicate transaction set uses these strings to look up 
    /// protocols, fields and filters in the protocol library.
    /// </summary>
    public class Predicate : IEquatable<Predicate>, IComparable<Predicate>
    {
        public OrString Value { get; private set; }
        public List<Predicate> Subpredicates { get; private set; }
        public List<FilterAtom> FilterTargets { get; private set; }
        public List<ProtocolAtom> ProtocolTargets { get; private set; }

        public int Layer { get; private set; }

        public int WriteIndex { get; set; }

        public PredicateTransaction WriteTransaction(PredOutLoc writeLocation)
        {
            return new PredicateTransaction(Value, new PredicateWriteTransaction(writeLocation, WriteIndex));
        }

        public Predicate(OrString value)
        {
            Value = value;
            Subpredicates = new List<Predicate>();
            FilterTargets = new List<FilterAtom>();
            ProtocolTargets = new List<ProtocolAtom>();
            Layer = 0;


            foreach (var atom in Value.List.SelectMany(and => and.List))
            {
                StoreAtom(atom);
            }
        }

        private void StoreAtom(PredicateAtom atom)
        {
            switch (atom.AtomType)
            {
                case PredicateAtomType.SubPredicate:
                    var tmp = ((SubPredicateAtom)atom).Predicate;
                    Layer = Math.Max(tmp.Subpredicates.Select(s => s.Layer).Max() + 1, Layer);
                    Subpredicates.AddRange(tmp.Subpredicates);
                    Subpredicates.Add(tmp);
                    break;
                case PredicateAtomType.Protocol:
                    ProtocolTargets.Add((ProtocolAtom)atom);
                    break;
                case PredicateAtomType.Filter:
                    FilterTargets.Add((FilterAtom)atom);
                    break;
                case PredicateAtomType.Not:
                    StoreAtom(((NotAtom)atom).Operand);
                    break;
            }
        }

        public bool Equals(Predicate other)
        {
            if (Value.List.Count != other.Value.List.Count) return false;

            for (int k = 0; k < Value.List.Count; k++)
            {
                var and1 = Value.List[k];
                var and2 = other.Value.List[k];

                if (and1.List.Count != and2.List.Count) return false;

                for (int j = 0; j < and1.List.Count; j++)
                {
                   
                    var atom1 = and1.List[j];
                    var atom2 = and2.List[j];

                    if (atom1 != atom2) return false;
                     
                }
            }
            return true;
        }

        public int CompareTo(Predicate other)
        {
            return Subpredicates.Count.CompareTo(other.Subpredicates.Count);
        }
    }

    #region PredicateString
    
    public enum LogicOperator
    {
        None,
        Or,
        And,
        Not
    }

    public abstract class PredicateString<TElem>
    {
        public LogicOperator Operator { get; private set; }
        public List<TElem> List { get; private set; }

        protected PredicateString(LogicOperator op)
        {
            Operator = op;
            List = new List<TElem>();
        }

        public void AddElement(TElem node)
        {
            List.Add(node);
        }
    }

    public class OrString : PredicateString<AndString>
    {
        public OrString()
            : base(LogicOperator.Or)
        {
        }
    }

    public class AndString : PredicateString<PredicateAtom>
    {
        public AndString()
            : base(LogicOperator.And)
        {
        }
    }

    #endregion

    #region PredicateAtom

    public enum Comparison
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        GreaterThan = 3,
        LessThanOrEqual = 4,
        GreaterThanOrEqual = 5
    }

    public enum PredicateAtomType
    {
        Protocol,
        Filter,
        Not,
        SubPredicate,
        Comparison
    }

    public abstract class PredicateAtom :IEquatable<PredicateAtom>
    {
        public PredicateAtomType AtomType { get; protected set; }

        public abstract bool Equals(PredicateAtom other);
    }

    public class ProtocolAtom : PredicateAtom
    {
        public string Protocol { get; private set; }

        public ProtocolAtom(string protocol)
        {
            AtomType = PredicateAtomType.Protocol;
            this.Protocol = protocol;
        }

        public override bool Equals(PredicateAtom other)
        {
            if (other.AtomType != PredicateAtomType.Protocol) return false;

            var tmp = (ProtocolAtom) other;

            return Protocol == tmp.Protocol;

        }
    }


    public class FilterAtom : PredicateAtom
    {
        public string Protocol { get; private set; }
        public string Field { get; private set; }
        public string Filter { get; private set; }

        public FilterAtom(string protocol, string field, string filter)
        {
            AtomType = PredicateAtomType.Filter;
            this.Protocol = protocol;
            this.Field = field;
            this.Filter = filter;
        }

        public override bool Equals(PredicateAtom other)
        {
            if (other.AtomType != PredicateAtomType.Filter) return false;

            var tmp = (FilterAtom)other;

            return Protocol == tmp.Protocol && Field == tmp.Field && Filter == tmp.Filter;

        }
    }


    public class NotAtom : PredicateAtom
    {
        public PredicateAtom Operand { get; private set; }

        public NotAtom(PredicateAtom operand)
        {
            AtomType = PredicateAtomType.Not;
            this.Operand = operand;
        }

        public override bool Equals(PredicateAtom other)
        {
            if (other.AtomType != PredicateAtomType.Not) return false;

            var tmp = (NotAtom)other;

            return Operand == tmp.Operand;

        }
    }

    public class SubPredicateAtom : PredicateAtom
    {
        public Predicate Predicate { get; private set; }

        public SubPredicateAtom(OrString predicate)
        {
            Predicate = new Predicate(predicate);
            AtomType = PredicateAtomType.SubPredicate;
        }

        public override bool Equals(PredicateAtom other)
        {
            if (other.AtomType != PredicateAtomType.SubPredicate) return false;

            var tmp = (SubPredicateAtom)other;

            return Predicate == tmp.Predicate;

        }
    }

#endregion
}

using System;
using System.Collections.Generic;

namespace Grammar
{
    /// <summary>
    /// Contains the predicate specified for a kernel filter, and all its sub predicates.
    /// All predicate transaction sets write a final result to result memory - the rest 
    /// is stored in working memory.
    /// </summary>
    public class PredicateTransactionSet
    {
        public List<PredicateTransaction> Transactions;


        public PredicateTransactionSet(Predicate predicate)
        {
            this.Transactions = new List<PredicateTransaction>();
            if (predicate.Layer > 0)
            {
                predicate.Subpredicates.Sort();

                foreach (var s in predicate.Subpredicates)
                {
                    s.WriteIndex = MemoryCoordinator.GetBoolRuleIndex();
                    Transactions.Add(s.WriteTransaction(PredOutLoc.BoolRuleMemory));
                }
            }
            predicate.WriteIndex = MemoryCoordinator.GetBoolResultIndex();
            Transactions.Add(predicate.WriteTransaction(PredOutLoc.BoolResultMemory));
        }

        public List<byte> ToByteCode()
        {
            var tmp = new List<byte> { (byte)Transactions.Count };

            foreach (var transaction in Transactions)
            {
                tmp.AddRange(transaction.ToByteCode());
            }
            return tmp;
        }
    }

    public class PredicateTransaction
    {
        public OrTransaction Predicate { get; private set; }
        public PredicateWriteTransaction Transaction { get; private set; }

        public PredicateTransaction(OrString predicate, PredicateWriteTransaction transaction)
        {
            this.Transaction = transaction;
            this.Predicate = new OrTransaction(predicate);
        }

        public List<byte> ToByteCode()
        {
            //get expression code
            var tmp = Predicate.ToByteCode();

            //write destination
            tmp.AddRange(Transaction.ToByteCode());

            return tmp;
        }
    }

    public class OrTransaction
    {
        public List<AndTransaction> Transactions { get; private set; }

        public OrTransaction(OrString input)
        {
            this.Transactions = new List<AndTransaction>();
            foreach (var a in input.List)
            {
                Transactions.Add(new AndTransaction(a));
            }
        }

        public List<byte> ToByteCode()
        {
            var tmp = new List<byte> { (byte)Transactions.Count };

            foreach (var transaction in Transactions)
            {
                tmp.AddRange(transaction.ToByteCode());
            }

            return tmp;
        }
    }

    public class AndTransaction
    {
        public List<PredicateReadTransaction> Transactions { get; private set; }

        public AndTransaction(AndString input)
        {
            this.Transactions = new List<PredicateReadTransaction>();
            for (int index = 0; index < input.List.Count; index++)
            {
                var atom = input.List[index];
                bool invert = false;
                PredicateReadTransaction tmp;
                if (atom.AtomType == PredicateAtomType.Not)
                {
                    atom = ((NotAtom) atom).Operand;
                    invert = true;
                }

                switch (atom.AtomType)
                {
                    case PredicateAtomType.SubPredicate:
                        var pred = ((SubPredicateAtom) atom);
                        tmp = new PredicateReadTransaction(PredInLoc.BoolRuleMemory, pred.Predicate.WriteIndex, invert);
                        break;

                    case PredicateAtomType.Protocol:
                        var proto = ProtocolLibrary.GetProtocol(((ProtocolAtom) atom).Protocol);
                        tmp = new PredicateReadTransaction(PredInLoc.BoolRuleMemory, proto.RuleIndex);
                        break;

                    case PredicateAtomType.Filter:
                        var filter = ((FilterAtom) atom);
                        var f = ProtocolLibrary.GetFieldFilter(filter.Protocol, filter.Field, filter.Filter);
                        tmp = new PredicateReadTransaction(PredInLoc.BoolRuleMemory, f.RuleIndex);
                        break;  

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Transactions.Add(tmp);
            }
        }

        public List<byte> ToByteCode()
        {
            var tmp = new List<byte>() { (byte)Transactions.Count };
            foreach (var read in Transactions)
            {
                tmp.AddRange(read.ToByteCode());
            }

            return tmp;
        }
    }

    public class PredicateReadTransaction
    {
        public PredInLoc Location { get; private set; }
        public int Index { get; private set; }
        public bool Invert { get; private set; }

        public PredicateReadTransaction(PredInLoc location, int index, bool invert = false)
        {
            this.Location = location;
            this.Index = index;
            Invert = invert;
        }

        public List<byte> ToByteCode()
        {
            var tmp = new List<byte> { Invert ? (byte)1 : (byte)0, (byte)Index };
            return tmp;
        }
    }

    public class PredicateWriteTransaction
    {
        public PredOutLoc Location { get; private set; }
        public int Index { get; private set; }

        public PredicateWriteTransaction(PredOutLoc location, int index)
        {
            this.Location = location;
            this.Index = index;
        }

        public List<byte> ToByteCode()
        {
            var tmp = new List<byte> { (byte)Location, (byte)Index };
            return tmp;
        }
    }
    public enum PredInLoc
    {
        BoolRuleMemory,
        BoolResultMemory
    }

    public enum PredOutLoc
    {
        BoolRuleMemory,
        BoolResultMemory
    }
}
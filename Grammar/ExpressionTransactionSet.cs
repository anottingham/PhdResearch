using System;
using System.Collections.Generic;

namespace Grammar
{
    public class ExpressionTransactionSet
    {
        public List<ExpressionTransaction> Transactions;
        public int RegisterMemory { get; private set; }


        public ExpressionTransactionSet(Expression expression, ExpressionWriteTransaction write)
        {
            this.Transactions = new List<ExpressionTransaction>();
            if (expression.Layer > 0)
            {
                //allow 1 register for each subexpression
                RegisterMemory = expression.Subexpressions.Count;
                expression.Subexpressions.Sort();

                for (var index = 0; index < expression.Subexpressions.Count; index++)
                {
                    var s = expression.Subexpressions[index];
                    s.RegisterIndex = index+1;
                    var tmp = new ExpressionTransaction(s.Value,
                        new ExpressionWriteTransaction(ExprOutLoc.RegisterBank, s.RegisterIndex));
                    Transactions.Add(tmp);
                }
            }
            else {RegisterMemory = 1;}

            //write the final output of the expression tot he first register
            expression.RegisterIndex = 0;
            Transactions.Add(new ExpressionTransaction(expression.Value, write));
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
    public class ExpressionTransaction
    {
        public AddTransaction Expression { get; private set; }
        public ExpressionWriteTransaction Transaction { get; private set; }

        public ExpressionTransaction(AdditionString expression, ExpressionWriteTransaction transaction)
        {
            this.Transaction = transaction;
            this.Expression = new AddTransaction(expression);
        }

        public List<byte> ToByteCode()
        {
            //get expression code
            var tmp = Expression.ToByteCode();

            //write destination
            tmp.AddRange(Transaction.ToByteCode());

            return tmp;
        }
    }

    public class AddTransaction
    {
        public List<MultTransaction> Transactions { get; private set; }

        public AddTransaction(AdditionString input)
        {
            this.Transactions = new List<MultTransaction>();
            foreach (var a in input.List)
            {
                if (a.List.Count == 1)
                {
                    Transactions.Add(new MultTransaction(a.List[0]));
                }
                else throw new Exception("AddTransaction Error: Subtraction not supported in simple strings");

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

    public class MultTransaction
    {
        public List<ExpressionReadTransaction> Transactions { get; private set; }

        public MultTransaction(MultiplicationString input)
        {
            this.Transactions = new List<ExpressionReadTransaction>();
            foreach (var a in input.List)
            {
                if (a.List.Count == 1)
                {
                    ExpressionAtom atom = a.List[0];
                    ExpressionReadTransaction tmp;
                    switch (atom.AtomType)
                    {
                        case ExpressionAtomType.Static:
                            var stat = (StaticAtom)atom;
                            tmp = new ExpressionReadTransaction(ExprInLoc.Lookup, MemoryCoordinator.RegisterStaticInteger(stat.Value));
                            break;
                        case ExpressionAtomType.Register:
                            var sysreg = (RegisterAtom)atom;
                            tmp = new ExpressionReadTransaction(ExprInLoc.SystemReg, sysreg.RegisterValue);
                            break;
                        case ExpressionAtomType.SubExpression:
                            var sub = (SubExpressionAtom)atom;
                            tmp = new ExpressionReadTransaction(ExprInLoc.RegisterBank, sub.Expression.RegisterIndex);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Transactions.Add(tmp);
                }
                else throw new Exception("AddTransaction Error: Subtraction not supported in simple strings");

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

    public class ExpressionReadTransaction
    {
        public ExprInLoc Location { get; private set; }
        public int Index { get; private set; }

        public ExpressionReadTransaction(ExprInLoc location, int index)
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

    public class ExpressionWriteTransaction
    {
        public ExprOutLoc Location { get; private set; }
        public int Index { get; private set; }

        public ExpressionWriteTransaction(ExprOutLoc location, int index)
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
    public enum ExprInLoc
    {
        RegisterBank,   //value = bank index
        SystemReg,      //value = 0 -> Length, 1 -> Value
        Lookup          //value = static value
    }

    public enum ExprOutLoc
    {
        RegisterBank = 0,
        SystemReg = 1
    }
}
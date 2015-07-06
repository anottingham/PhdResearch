using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Grammar
{

    /// <summary>
    /// Class containing all the necessary 
    /// </summary>
    public class Expression : IEquatable<Expression>, IComparable<Expression>
    {
        public AdditionString Value { get; private set; }
        public List<Expression> Subexpressions { get; private set; }
        public List<RegisterAtom> RegisterTargets { get; private set; }

        public int Layer { get; private set; }
        public int RegisterIndex { get; set; }


        /// <summary>
        /// Inidcates if the expression is simple (requires only '+' and '*')
        /// Simple expressions are processed require fewer op codes
        /// </summary>
        public bool Simple { get; private set; }
        
        public Expression(AdditionString value)
        {
            Value = value;
            Subexpressions = new List<Expression>();
            RegisterTargets = new List<RegisterAtom>();
            Simple = true;
            Layer = 0;
            InitialiseExpression();
        }

        private void InitialiseExpression()
        {
            foreach (SubtractionString sub in Value.List)
            {
                if (sub.List.Count > 1) Simple = false;
                foreach (MultiplicationString mult in sub.List)
                {
                    foreach (DivisionString div in mult.List)
                    {
                        if (div.List.Count > 1) Simple = false;
                        foreach (ExpressionAtom atom in div.List)
                        {
                            switch (atom.AtomType)
                            {
                                case ExpressionAtomType.SubExpression:
                                    Expression tmp = ((SubExpressionAtom) atom).Expression;
                                    Layer = Math.Max(tmp.Subexpressions.Select(s => s.Layer).Max() + 1, Layer);
                                    Subexpressions.AddRange(tmp.Subexpressions);
                                    Subexpressions.Add(tmp);
                                    break;
                                case ExpressionAtomType.Register:
                                    RegisterTargets.Add((RegisterAtom)atom);
                                    break;
                                case ExpressionAtomType.Static:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public ExpressionTransactionSet GenerateExpressionTransactions(ExpressionWriteTransaction write)
        {
            if (!Simple) throw new Exception("Only simple equations (+,*) allowed in this build.");
            return new ExpressionTransactionSet(this, write);
        }

        public bool Equals(Expression other)
        {
            if (Value.List.Count != other.Value.List.Count) return false;

            for(var k = 0; k < Value.List.Count; k++)
            {
                var sub1 = Value.List[k];
                var sub2 = other.Value.List[k];

                if (sub1.List.Count != sub2.List.Count) return false;

                for (var j = 0; j < sub1.List.Count; j++)
                {
                    var mul1 = sub1.List[j];
                    var mul2 = sub2.List[j];

                    if (mul1.List.Count != mul2.List.Count) return false;
                    
                    for (var m = 0; m < mul1.List.Count; m++)
                    {
                        var div1 = mul1.List[m];
                        var div2 = mul2.List[m];

                        if (div1.List.Count != div2.List.Count) return false;
                        
                        for (var n = 0; n < div1.List.Count; n++)
                        {
                            var atom1 = div1.List[n];
                            var atom2 = div2.List[n];

                            if (atom1 != atom2) return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Used for sorting subfilter lists. Uses the number of subequations an expression has to determine processing order.
        /// As the number of subexpressions grows with each move away from leaf expressions, dependand protocols will always have at least 
        /// 1 more subexpression than its closest child. Those with the least subexpressions naturally move to the head of the liust when sorted.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Expression other)
        {
            return Subexpressions.Count.CompareTo(other.Subexpressions.Count);
        }
    }

    

    #region ExpressionString

    public enum MathOperator
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TElem"> The atom type of each element (</typeparam>
    /// <typeparam name="TOps">The operation type repres</typeparam>
    public abstract class ExpressionString<TElem>
    {
        public List<TElem> List { get; private set; }
        public MathOperator Operator { get; private set; }

        protected ExpressionString(MathOperator op)
        {
            Operator = op;
            List = new List<TElem>();
        }

        public void AddElement(TElem node)
        {
            List.Add(node);
        }

    }

    public class AdditionString : ExpressionString<SubtractionString>
    {
        public AdditionString() : base(MathOperator.Add) {}
    }

    public class SubtractionString : ExpressionString<MultiplicationString>
    {
        public SubtractionString() : base(MathOperator.Subtract) {}
    }

    public class MultiplicationString : ExpressionString<DivisionString>
    {
        public MultiplicationString() : base(MathOperator.Multiply) { }
    }

    public class DivisionString : ExpressionString<ExpressionAtom>
    {
        public DivisionString() : base(MathOperator.Divide) { }
    }

#endregion

#region ExpressionAtom

    public enum SystemRegister
    {
        Length = 0,
        Value = 1,
    }

    public enum ExpressionAtomType
    {
        Static,
        Register,
        SubExpression
    }

    public abstract class ExpressionAtom : IEquatable<ExpressionAtom>
    {
        public ExpressionAtomType AtomType { get; protected set; }
        public abstract bool Equals(ExpressionAtom other);
    }
    
    public class StaticAtom : ExpressionAtom
    {
        public int Value { get; private set; }

        public StaticAtom(int value)
        {
            AtomType = ExpressionAtomType.Static;
            this.Value = value;
        }

        public override bool Equals(ExpressionAtom other)
        {
            if (AtomType != other.AtomType) return false;
            var tmp = (StaticAtom)other;
            return Value == tmp.Value;
        }
    }


    public class RegisterAtom : ExpressionAtom
    {
        public SystemRegister Register { get; private set; }
        public int RegisterValue { get { return (int) Register; }}

        public RegisterAtom(SystemRegister register)
        {
            AtomType = ExpressionAtomType.Register;
            this.Register = register;
        }

        public override bool Equals(ExpressionAtom other)
        {
            if (AtomType != other.AtomType) return false;
            var tmp = (RegisterAtom)other;
            return Register == tmp.Register;
        }
    }

    public class SubExpressionAtom : ExpressionAtom
    {
        public Expression Expression { get; private set; }

        public SubExpressionAtom(AdditionString expression)
        {
            Expression = new Expression(expression);
            AtomType = ExpressionAtomType.SubExpression;
        }

        public override bool Equals(ExpressionAtom other)
        {
            if (AtomType != other.AtomType) return false;
            var tmp = (SubExpressionAtom)other;
            return Expression == tmp.Expression;
        }
    }


#endregion

}

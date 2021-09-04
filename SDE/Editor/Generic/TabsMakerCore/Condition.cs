using GRF.FileFormats.LubFormat;
using SDE.Editor.Engines;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.TabsMakerCore
{
    public class BooleanCondition : Condition
    {
        public bool Value { get; set; }

        public BooleanCondition(string value)
        {
            if (value == "true")
                Value = true;
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            Value = !Value;
        }

        protected override string _getStringValue()
        {
            return Value ? "true" : "false";
        }

        public override Condition Copy()
        {
            return new BooleanCondition(Value.ToString()) { Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            return new Func<TValue, string, bool>((t, s) => Value);
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = ToPredicate(settings);
            return (t, s) => predicate(t, s).ToString();
        }
    }

    public class RelationalCondition : Condition
    {
        private RelationalComparison _comparison = RelationalComparison.None;
        private Condition _leftCondition;
        private Condition _rightCondition;

        public RelationalCondition(Condition left, RelationalComparison comparison, Condition right)
        {
            left = ConditionLogic.GetCondition(left);
            right = ConditionLogic.GetCondition(right);

            _leftCondition = left;
            _rightCondition = right;
            _comparison = comparison;

            Suffix = left.Suffix;
            Prefix = left.Prefix;

            left.Prefix = "";
            left.Suffix = "";
            right.Prefix = "";
            right.Suffix = "";

            RelationalCondition rLeft = left as RelationalCondition;
            if (rLeft != null)
            {
                RelationalComparison comp = rLeft._comparison;

                if ((comp == RelationalComparison.And || comp == RelationalComparison.Or) &&
                    (comparison == RelationalComparison.And || comparison == RelationalComparison.Or) &&
                    rLeft._comparison != comparison)
                {
                    _leftCondition = new ParenthesisCondition(rLeft);
                }
            }

            RelationalCondition rRight = right as RelationalCondition;
            if (rRight != null)
            {
                RelationalComparison comp = rRight._comparison;

                if ((comp == RelationalComparison.And || comp == RelationalComparison.Or) &&
                    (comparison == RelationalComparison.And || comparison == RelationalComparison.Or) &&
                    rRight._comparison != comparison)
                {
                    _rightCondition = new ParenthesisCondition(rRight);
                }
            }
        }

        public RelationalCondition(string left, string comparison, string right)
        {
            _init(left, comparison, right);
        }

        public RelationalCondition(string left, string rightSide)
        {
            _init(left, rightSide);
        }

        public RelationalCondition(string full)
        {
            // The left side will never have parenthesis
            full = full.Replace(" and ", "&&").Replace(" or ", "||");
            int startIndex = 0;

            while (!_isConditionCharacter(full[startIndex]))
            {
                startIndex++;
            }

            startIndex--;

            _init(full.Substring(0, startIndex), full.Substring(startIndex));
        }

        public override Condition Copy()
        {
            return new RelationalCondition(_leftCondition.Copy(), _comparison, _rightCondition.Copy()) { Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt)
        {
            isInt = false;

            if (_comparison >= RelationalComparison.BinaryAnd)
            {
                isInt = true;
                bool int2;
                var predicateLeft = _leftCondition.ToInt(settings, out int2);
                var predicateRight = _rightCondition.ToInt(settings, out int2);

                switch (_comparison)
                {
                    case RelationalComparison.BinaryAnd:
                        return new Func<TValue, string, double>((t, s) => (int)predicateLeft(t, s) & (int)predicateRight(t, s));

                    case RelationalComparison.BinaryOr:
                        return new Func<TValue, string, double>((t, s) => (int)predicateLeft(t, s) | (int)predicateRight(t, s));

                    case RelationalComparison.BinaryRightShift:
                        return new Func<TValue, string, double>((t, s) => (int)predicateLeft(t, s) >> (int)predicateRight(t, s));

                    case RelationalComparison.BinaryLeftShift:
                        return new Func<TValue, string, double>((t, s) => (int)predicateLeft(t, s) << (int)predicateRight(t, s));

                    case RelationalComparison.Add:
                        return new Func<TValue, string, double>((t, s) => predicateLeft(t, s) + predicateRight(t, s));

                    case RelationalComparison.Minus:
                        return new Func<TValue, string, double>((t, s) => predicateLeft(t, s) - predicateRight(t, s));

                    case RelationalComparison.Mult:
                        return new Func<TValue, string, double>((t, s) => predicateLeft(t, s) * predicateRight(t, s));

                    case RelationalComparison.Div:
                        return new Func<TValue, string, double>((t, s) => predicateLeft(t, s) / predicateRight(t, s));

                    case RelationalComparison.Mod:
                        return new Func<TValue, string, double>((t, s) => predicateLeft(t, s) % (int)predicateRight(t, s));

                    case RelationalComparison.Pow:
                        return new Func<TValue, string, double>((t, s) => Math.Pow(predicateLeft(t, s), predicateRight(t, s)));
                }
            }

            return new Func<TValue, string, double>((t, s) => 0);
        }

        public override Func<long> ToLong(FlagTypeData settings)
        {
            if (_comparison >= RelationalComparison.BinaryAnd)
            {
                var predicateLeft = _leftCondition.ToLong(settings);
                var predicateRight = _rightCondition.ToLong(settings);

                switch (_comparison)
                {
                    case RelationalComparison.BinaryAnd:
                        return new Func<long>(() => predicateLeft() & predicateRight());

                    case RelationalComparison.BinaryOr:
                        return new Func<long>(() => predicateLeft() | predicateRight());

                    case RelationalComparison.BinaryRightShift:
                        return new Func<long>(() => predicateLeft() >> (int)predicateRight());

                    case RelationalComparison.BinaryLeftShift:
                        return new Func<long>(() => predicateLeft() << (int)predicateRight());

                    case RelationalComparison.Add:
                        return new Func<long>(() => predicateLeft() + predicateRight());

                    case RelationalComparison.Minus:
                        return new Func<long>(() => predicateLeft() - predicateRight());

                    case RelationalComparison.Mult:
                        return new Func<long>(() => predicateLeft() * predicateRight());

                    case RelationalComparison.Div:
                        return new Func<long>(() => predicateLeft() / predicateRight());

                    case RelationalComparison.Mod:
                        return new Func<long>(() => predicateLeft() % predicateRight());
                }
            }

            return new Func<long>(() => 0);
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            Func<TValue, string, bool> leftCondition = (t, s) => false;
            Func<TValue, string, bool> rightCondition = (t, s) => false;
            Func<TValue, string, double> left = (t, s) => 0;
            Func<TValue, string, double> right = (t, s) => 0;
            Func<TValue, string, string> valueLeft = (t, s) => "";
            Func<TValue, string, string> valueRight = (t, s) => "";
            bool isLeftInt = false;
            bool isRightInt = false;

            if (_leftCondition != null)
            {
                leftCondition = _leftCondition.ToPredicate(settings);
                left = _leftCondition.ToInt(settings, out isLeftInt);
                valueLeft = _leftCondition.ToValue(settings);
            }

            if (_rightCondition != null)
            {
                rightCondition = _rightCondition.ToPredicate(settings);
                right = _rightCondition.ToInt(settings, out isRightInt);
                valueRight = _rightCondition.ToValue(settings);
            }

            switch (_comparison)
            {
                case RelationalComparison.And:
                    return (t, s) => leftCondition(t, s) && rightCondition(t, s);

                case RelationalComparison.Or:
                    return (t, s) => leftCondition(t, s) || rightCondition(t, s);

                case RelationalComparison.Contains:
                    if (isLeftInt || isRightInt)
                    {
                        return (t, s) => left(t, s).ToString(CultureInfo.InvariantCulture).IndexOf(right(t, s).ToString(CultureInfo.InvariantCulture), 0, StringComparison.OrdinalIgnoreCase) > -1;
                    }

                    return (t, s) => valueLeft(t, s).IndexOf(valueRight(t, s), 0, StringComparison.OrdinalIgnoreCase) > -1;

                case RelationalComparison.Exclude:
                    if (isLeftInt || isRightInt)
                    {
                        return (t, s) => left(t, s).ToString(CultureInfo.InvariantCulture).IndexOf(right(t, s).ToString(CultureInfo.InvariantCulture), 0, StringComparison.OrdinalIgnoreCase) == -1;
                    }

                    return (t, s) => valueLeft(t, s).IndexOf(valueRight(t, s), 0, StringComparison.OrdinalIgnoreCase) == -1;

                case RelationalComparison.Eq:
                    if (isLeftInt || isRightInt)
                    {
                        return (t, s) => left(t, s) == right(t, s);
                    }

                    return (t, s) => String.Compare(valueLeft(t, s), valueRight(t, s), StringComparison.OrdinalIgnoreCase) == 0;

                case RelationalComparison.NotEq:
                    if (isLeftInt || isRightInt)
                    {
                        return (t, s) => left(t, s) != right(t, s);
                    }

                    return (t, s) => String.Compare(valueLeft(t, s), valueRight(t, s), StringComparison.OrdinalIgnoreCase) != 0;

                case RelationalComparison.Ge:
                    return (t, s) => left(t, s) >= right(t, s);

                case RelationalComparison.Le:
                    return (t, s) => left(t, s) <= right(t, s);

                case RelationalComparison.Gt:
                    return (t, s) => left(t, s) > right(t, s);

                case RelationalComparison.Lt:
                    return (t, s) => left(t, s) < right(t, s);
            }

            return (t, s) => false;
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = ToPredicate(settings);
            return (t, s) => predicate(t, s).ToString();
        }

        private void _init(string left, string rightSide)
        {
            rightSide = rightSide.Replace(" and ", "&&").Replace(" or ", "||");
            int startIndex = 0;

            while (_isConditionCharacter(rightSide[startIndex]))
            {
                startIndex++;
            }

            startIndex--;

            _init(left, rightSide.Substring(0, startIndex), rightSide.Substring(startIndex));
        }

        private void _init(string left, string comparison, string right)
        {
            _leftCondition = ConditionLogic.GetCondition(left);
            _readComparison(comparison);
            _rightCondition = ConditionLogic.GetCondition(right);
        }

        private void _readComparison(string comparison)
        {
            string condition = comparison.Replace("and", "&&").Replace("or", "||").Trim(' ');

            switch (condition)
            {
                case "<":
                    _comparison = RelationalComparison.Lt;
                    break;

                case ">":
                    _comparison = RelationalComparison.Gt;
                    break;

                case "<=":
                    _comparison = RelationalComparison.Le;
                    break;

                case ">=":
                    _comparison = RelationalComparison.Ge;
                    break;

                case "==":
                    _comparison = RelationalComparison.Eq;
                    break;

                case "⊃":
                    _comparison = RelationalComparison.Contains;
                    break;

                case "⊅":
                    _comparison = RelationalComparison.Exclude;
                    break;

                case "~=":
                    _comparison = RelationalComparison.NotEq;
                    break;

                case "!=":
                    _comparison = RelationalComparison.NotEq;
                    break;

                case "&&":
                    _comparison = RelationalComparison.And;
                    break;

                case "||":
                    _comparison = RelationalComparison.Or;
                    break;

                case "+":
                    _comparison = RelationalComparison.Add;
                    break;

                case "-":
                    _comparison = RelationalComparison.Minus;
                    break;

                case "*":
                    _comparison = RelationalComparison.Mult;
                    break;

                case "/":
                    _comparison = RelationalComparison.Div;
                    break;

                case "^":
                    _comparison = RelationalComparison.Pow;
                    break;

                case "%":
                    _comparison = RelationalComparison.Mod;
                    break;

                case "&":
                    _comparison = RelationalComparison.BinaryAnd;
                    break;

                case "|":
                    _comparison = RelationalComparison.BinaryOr;
                    break;

                case "~":
                    _comparison = RelationalComparison.Not;
                    break;

                case ">>":
                    _comparison = RelationalComparison.BinaryRightShift;
                    break;

                case "<<":
                    _comparison = RelationalComparison.BinaryLeftShift;
                    break;
            }
        }

        private bool _isConditionCharacter(char c)
        {
            return c == '=' || c == '<' || c == '>' || c == '~' || c == '&' || c == '|' || c == '+' || c == '/' || c == '*' || c == '-' || c == '%' || c == '^' || c == ' ';
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            deep--;

            switch (_comparison)
            {
                case RelationalComparison.And:
                case RelationalComparison.Or:
                    _leftCondition.Reverse(deep);
                    _comparison = _comparison == RelationalComparison.Or ? RelationalComparison.And : RelationalComparison.Or;
                    _rightCondition.Reverse(deep);
                    break;

                case RelationalComparison.Eq:
                    _comparison = RelationalComparison.NotEq;
                    break;

                case RelationalComparison.NotEq:
                    _comparison = RelationalComparison.Eq;
                    break;

                case RelationalComparison.Contains:
                    _comparison = RelationalComparison.Exclude;
                    break;

                case RelationalComparison.Exclude:
                    _comparison = RelationalComparison.Contains;
                    break;

                case RelationalComparison.Ge:
                    _comparison = RelationalComparison.Lt;
                    break;

                case RelationalComparison.Lt:
                    _comparison = RelationalComparison.Ge;
                    break;

                case RelationalComparison.Gt:
                    _comparison = RelationalComparison.Le;
                    break;

                case RelationalComparison.Le:
                    _comparison = RelationalComparison.Gt;
                    break;
            }
        }

        protected override string _getStringValue()
        {
            if (_comparison == RelationalComparison.Eq || _comparison == RelationalComparison.NotEq)
            {
                BooleanCondition bC;

                // Simplify cases, such as...
                // condition == true > condition
                //
                if (_comparison == RelationalComparison.Eq)
                {
                    Func<BooleanCondition, Condition, string> simplify = new Func<BooleanCondition, Condition, string>((condBool, cond2) =>
                    {
                        if (condBool.Value)
                        {
                            return cond2;
                        }
                        cond2.Reverse();
                        return cond2;
                    });

                    bC = _leftCondition as BooleanCondition;
                    if (bC != null) return simplify(bC, _rightCondition);
                    bC = _rightCondition as BooleanCondition;
                    if (bC != null) return simplify(bC, _leftCondition);
                }
                else if (_comparison == RelationalComparison.NotEq)
                {
                    Func<BooleanCondition, Condition, string> simplify = new Func<BooleanCondition, Condition, string>((condBool, cond2) =>
                    {
                        if (!condBool.Value)
                        {
                            return cond2;
                        }
                        cond2.Reverse();
                        return cond2;
                    });

                    bC = _leftCondition as BooleanCondition;
                    if (bC != null) return simplify(bC, _rightCondition);
                    bC = _rightCondition as BooleanCondition;
                    if (bC != null) return simplify(bC, _leftCondition);
                }
            }

            return _leftCondition + RelationToString(_comparison) + _rightCondition;
        }

        public static string RelationToString(RelationalComparison comparison)
        {
            switch (comparison)
            {
                case RelationalComparison.None:
                    return "";

                case RelationalComparison.Le:
                    return " <= ";

                case RelationalComparison.Lt:
                    return " < ";

                case RelationalComparison.Eq:
                    return " == ";

                case RelationalComparison.NotEq:
                    return " ~= ";

                case RelationalComparison.Contains:
                    return " ⊃ ";

                case RelationalComparison.Exclude:
                    return " ⊅ ";

                case RelationalComparison.Ge:
                    return " >= ";

                case RelationalComparison.Gt:
                    return " > ";

                case RelationalComparison.And:
                    return " and ";

                case RelationalComparison.Or:
                    return " or ";

                case RelationalComparison.Add:
                    return " + ";

                case RelationalComparison.Minus:
                    return " - ";

                case RelationalComparison.Mult:
                    return " * ";

                case RelationalComparison.Div:
                    return " / ";

                case RelationalComparison.Pow:
                    return " ^ ";

                case RelationalComparison.Mod:
                    return " % ";

                case RelationalComparison.BinaryAnd:
                    return " & ";

                case RelationalComparison.BinaryOr:
                    return " | ";

                case RelationalComparison.Not:
                    return " ~";

                case RelationalComparison.BinaryRightShift:
                    return " >> ";

                case RelationalComparison.BinaryLeftShift:
                    return " << ";

                default:
                    return "";
            }
        }
    }

    public class ParenthesisCondition : Condition
    {
        public bool IsReversed { get; private set; }
        public Condition Condition { get; set; }

        public ParenthesisCondition(string value)
        {
            Condition = ConditionLogic.GetCondition(value);
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            IsReversed = !IsReversed;
        }

        protected override string _getStringValue()
        {
            return IsReversed ? "not(" + Condition + ")" : "(" + Condition + ")";
        }

        public override Condition Copy()
        {
            return new ParenthesisCondition(Condition.Copy()) { IsReversed = this.IsReversed, Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = Condition.ToPredicate(settings);
            return new Func<TValue, string, bool>((t, s) => IsReversed ? !predicate(t, s) : predicate(t, s));
        }

        public override Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt)
        {
            var predicate = Condition.ToInt(settings, out isInt);
            return predicate;
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = ToPredicate(settings);
            return (t, s) => predicate(t, s).ToString();
        }
    }

    public class VariableCondition : Condition
    {
        public bool IsReversed { get; private set; }
        public string Value { get; set; }

        public VariableCondition(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (ConditionLogic.IsConditionCharacterWithoutSpace(value[i]))
                    throw new Exception("Invalid character " + value[i]);
            }

            Value = value;
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            IsReversed = !IsReversed;
        }

        protected override string _getStringValue()
        {
            return IsReversed ? "not(" + Value + ")" : Value;
        }

        public override Condition Copy()
        {
            return new VariableCondition(Value) { IsReversed = this.IsReversed, Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            if (Value.StartsWith("[") && Value.EndsWith("]"))
            {
                string se = Value.Substring(1, Value.Length - 2);

                var att = settings.AttributeList.Find(se);

                if (att >= 0)
                {
                    return new Func<TValue, string, bool>((t, s) =>
                    {
                        string val = t.GetValue<string>(att);
                        bool ival2;
                        Boolean.TryParse(val, out ival2);
                        return ival2;
                    });
                }

                return new Func<TValue, string, bool>((t, s) => false);
            }

            bool b;
            Boolean.TryParse(Value, out b);
            return new Func<TValue, string, bool>((t, s) => b);
        }

        public override Func<long> ToLong(FlagTypeData settings)
        {
            return new Func<long>(() => settings.Name2Value[Value]);
        }

        public override Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt)
        {
            isInt = false;

            if (Value.StartsWith("[") && Value.EndsWith("]"))
            {
                string se = Value.Substring(1, Value.Length - 2);

                var att = settings.AttributeList.Find(se);
                bool? canDirect = null;

                if (att >= 0)
                {
                    return new Func<TValue, string, double>((t, s) =>
                    {
                        int ival2;

                        if (canDirect == true)
                        {
                            try
                            {
                                ival2 = t.GetValue<int>(att);
                                return ival2;
                            }
                            catch
                            {
                                canDirect = false;
                            }
                        }

                        string val = t.GetValue<string>(att);

                        if (canDirect == null)
                        {
                            try
                            {
                                ival2 = t.GetValue<int>(att);
                                return ival2;
                            }
                            catch
                            {
                                canDirect = false;
                            }
                        }

                        if (!Int32.TryParse(val, out ival2))
                        {
                            if (val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            {
                                ival2 = FormatConverters.IntOrHexConverter(val);
                            }
                        }

                        return ival2;
                    });
                }

                return new Func<TValue, string, double>((t, s) => 0);
            }

            if (Value.StartsWith("Flags."))
            {
                string f = Value.ReplaceFirst("Flags.", "");
                isInt = true;
                long val = FlagsManager.GetFlagValue(f);
                return new Func<TValue, string, double>((t, s) => val);
            }

            double ival;
            if (double.TryParse(Value.Replace(".", ","), out ival))
            {
                isInt = true;
            }
            else if (double.TryParse(Value.Replace(",", "."), out ival))
            {
                isInt = true;
            }
            else
            {
                if (Value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    ival = FormatConverters.IntOrHexConverter(Value);
                    isInt = true;
                }
            }

            return new Func<TValue, string, double>((t, s) => ival);
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            if (Value.StartsWith("[") && Value.EndsWith("]"))
            {
                string se = Value.Substring(1, Value.Length - 2);

                var att = settings.AttributeList.Find(se);

                if (att >= 0)
                {
                    return new Func<TValue, string, string>((t, s) => t.GetValue<string>(att));
                }
            }

            if (Value.StartsWith("\"") && Value.EndsWith("\""))
            {
                Value = Value.Substring(1, Value.Length - 2);
            }

            return new Func<TValue, string, string>((t, s) => Value);
        }
    }

    public class NotCondition : Condition
    {
        public bool IsReversed { get; private set; }
        public Condition Condition { get; set; }

        public NotCondition(string value)
        {
            Condition = ConditionLogic.GetCondition(value);
        }

        public NotCondition(Condition value)
        {
            Condition = value;
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            IsReversed = !IsReversed;
        }

        protected override string _getStringValue()
        {
            NotCondition nC = Condition as NotCondition;
            if (nC != null)
            {
                if (!IsReversed && !nC.IsReversed)
                {
                    return Condition;
                }
            }

            if (IsReversed && !(Condition is RelationalCondition))
            {
                return Condition;
            }

            if (!IsReversed)
            {
                if (Condition is RelationalCondition || Condition is BooleanCondition)
                    return Condition.Reverse();
            }

            return IsReversed ? "(" + Condition + ")" : "not(" + Condition + ")";
        }

        public override Condition Copy()
        {
            return new NotCondition(Condition.Copy()) { IsReversed = this.IsReversed, Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = Condition.ToPredicate(settings);
            return new Func<TValue, string, bool>((t, s) => IsReversed ? predicate(t, s) : !predicate(t, s));
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = ToPredicate(settings);
            return (t, s) => predicate(t, s).ToString();
        }

        public override Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt)
        {
            var predicate = Condition.ToInt(settings, out isInt);
            return predicate;
        }
    }

    public class UnaryNotCondition : Condition
    {
        public bool IsReversed { get; private set; }
        public Condition Condition { get; set; }

        public UnaryNotCondition(string value)
        {
            Condition = ConditionLogic.GetCondition(value);
        }

        public UnaryNotCondition(Condition value)
        {
            Condition = value;
        }

        internal override void Reverse(int deep)
        {
            if (deep < 0) return;
            IsReversed = !IsReversed;
        }

        protected override string _getStringValue()
        {
            UnaryNotCondition nC = Condition as UnaryNotCondition;
            if (nC != null)
            {
                if (!IsReversed && !nC.IsReversed)
                {
                    return Condition;
                }
            }

            if (IsReversed && !(Condition is RelationalCondition))
            {
                return Condition;
            }

            if (!IsReversed)
            {
                if (Condition is RelationalCondition || Condition is BooleanCondition)
                    return Condition.Reverse();
            }

            return IsReversed ? Condition : Condition.Reverse();
        }

        public override Condition Copy()
        {
            return new UnaryNotCondition(Condition.Copy()) { IsReversed = this.IsReversed, Prefix = this.Prefix, Suffix = this.Suffix };
        }

        public override Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = Condition.ToPredicate(settings);
            return new Func<TValue, string, bool>((t, s) => IsReversed ? predicate(t, s) : !predicate(t, s));
        }

        public override Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings)
        {
            var predicate = ToPredicate(settings);
            return (t, s) => predicate(t, s).ToString();
        }

        public override Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt)
        {
            var predicate = Condition.ToInt(settings, out isInt);
            return new Func<TValue, string, double>((t, s) => IsReversed ? predicate(t, s) : ~(int)predicate(t, s));
        }

        public override Func<long> ToLong(FlagTypeData settings)
        {
            var predicate = Condition.ToLong(settings);
            return new Func<long>(() => IsReversed ? predicate() : ~predicate());
        }
    }

    public static class ConditionLogic
    {
        private static readonly string[] _prefixes = { "while ", "if " };
        private static readonly string[] _suffixes = { " do", " then" };

        public static Condition GetCondition(Condition value)
        {
            return value.Copy();
        }

        public static Condition GetCondition(string value)
        {
            string prefix;
            string suffix;

            value = _getAffix(value, _prefixes, out prefix);
            value = _getAffix(value, _suffixes, out suffix);

            Condition condition = _getCondition(value);

            condition.Prefix = prefix;
            condition.Suffix = suffix;
            return condition;
        }

        private static Condition _getCondition(string value)
        {
            Condition condition;

            if (value == "true" || value == "false")
            {
                condition = new BooleanCondition(value);
            }
            else
            {
                string[] values = CutBrackets(value);

                if (values.Length == 1 && values[0].StartsWith("("))
                {
                    condition = new ParenthesisCondition(values[0].Substring(1, values[0].Length - 2));
                }
                else if (values.Length == 1 && values[0].StartsWith("not(") && values[0].EndsWith(")"))
                {
                    condition = new NotCondition(values[0].Substring(4, values[0].Length - 5));
                }
                else if (values.Length == 1 && values[0].StartsWith("~"))
                {
                    condition = new UnaryNotCondition(values[0].Substring(1, values[0].Length - 1).Trim(' '));
                }
                else if (values.Length == 1)
                {
                    condition = new VariableCondition(values[0]);
                }
                else if (values.Length == 3)
                {
                    //if (values[0].StartsWith("(")) {
                    //	condition = new ParenthesisCondition(values[0].Substring(1, values[0].Length - 2));
                    //}
                    condition = new RelationalCondition(values[0], values[1], values[2]);
                }
                else
                {
                    // Tougher checkup...!
                    // Give priority to && and ||
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (values[i] == "&&" || values[i] == "||")
                        {
                            condition = new RelationalCondition(String.Join(" ", values.Take(i).ToArray()), values[i], "(" + String.Join(" ", values.Skip(i + 1).ToArray()) + ")");
                            return condition;
                        }
                    }

                    // Give priority to == and ~=
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (values[i] == "==" || values[i] == "~=")
                        {
                            condition = new RelationalCondition(String.Join(" ", values.Take(i).ToArray()), values[i], "(" + String.Join(" ", values.Skip(i + 1).ToArray()) + ")");
                            return condition;
                        }
                    }

                    for (int i = 1; i < values.Length; i++)
                    {
                        if (values[i] == ">" || values[i] == ">=" || values[i] == "<" || values[i] == "<=")
                        {
                            condition = new RelationalCondition(String.Join(" ", values.Take(i).ToArray()), values[i], "(" + String.Join(" ", values.Skip(i + 1).ToArray()) + ")");
                            return condition;
                        }
                    }

                    condition = new VariableCondition(value);
                }
            }

            return condition;
        }

        public static string[] CutBrackets(string value)
        {
            return Cut(value, '(', ')');
        }

        public static bool IsConditionCharacterWithoutSpace(char c)
        {
            return c == '=' || c == '<' || c == '>' || c == '~' || c == '&' || c == '|' || c == '+' || c == '/' || c == '*' || c == '-' || c == '%' || c == '^' || c == '⊃' || c == '⊅';
        }

        private static string _getAffix(string value, string[] affixes, out string oAffix)
        {
            int indent = LineHelper.GetIndent(value);

            foreach (string affix in affixes)
            {
                if (value.Contains(affix))
                {
                    oAffix = LineHelper.GenerateIndent(indent) + affix;
                    value = value.ReplaceOnce(oAffix, "");
                    return value;
                }
            }

            oAffix = null;
            return value;
        }

        public static Condition SetWhileLoop(Condition condition)
        {
            int indent = LineHelper.GetIndent(condition.Prefix);
            condition.Prefix = LineHelper.GenerateIndent(indent) + "while ";
            condition.Suffix = " do";
            return condition;
        }

        public static Condition SetElseIf(Condition condition)
        {
            condition.Prefix = LineHelper.ReplaceAfterIndent(condition.Prefix, "elseif ");
            return condition;
        }

        public static string[] Cut(string value, char start, char end)
        {
            List<string> values = new List<string>();
            int scope = 0;

            value = value.Replace(" and ", " && ").Replace(" or ", " || ");

            int startIndex;
            int endIndex = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == start)
                {
                    scope = 1;
                    i++;

                    while (scope > 0 && i < value.Length)
                    {
                        if (value[i] == start)
                            scope++;
                        if (value[i] == end)
                            scope--;
                        i++;
                    }

                    if (i >= value.Length - 1)
                    {
                        values.Add(value.Substring(endIndex, value.Length - endIndex));
                    }
                }
                else if (value[i] == '~' && i < value.Length - 1 && value[i + 1] != '=')
                {
                }
                else if (IsConditionCharacterWithoutSpace(value[i]))
                {
                    startIndex = i;

                    if (startIndex > endIndex)
                    {
                        values.Add(value.Substring(endIndex, startIndex - endIndex).Trim(' '));
                    }

                    while (IsConditionCharacterWithoutSpace(value[i]))
                    {
                        i++;

                        if (value[i] == '~')
                            break;
                    }

                    values.Add(value.Substring(startIndex, i - startIndex));
                    endIndex = i;
                }
                else if (i == value.Length - 1)
                {
                    values.Add(value.Substring(endIndex, value.Length - endIndex));
                }
            }

            return values.Select(p => p.Trim(' ')).ToArray();
        }
    }

    public abstract class Condition
    {
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public Condition Reverse()
        {
            Reverse(1);
            return this;
        }

        internal abstract void Reverse(int deep);

        protected abstract string _getStringValue();

        public override string ToString()
        {
            return Prefix + _getStringValue() + Suffix;
        }

        public static implicit operator string(Condition condition)
        {
            return condition.ToString();
        }

        public abstract Condition Copy();

        public abstract Func<TValue, string, bool> ToPredicate<TKey, TValue>(GTabSettings<TKey, TValue> settings) where TValue : Database.Tuple;

        public virtual Func<TValue, string, double> ToInt<TKey, TValue>(GTabSettings<TKey, TValue> settings, out bool isInt) where TValue : Database.Tuple
        {
            isInt = false;
            return new Func<TValue, string, double>((t, s) => 0);
        }

        public virtual Func<long> ToLong(FlagTypeData settings)
        {
            return new Func<long>(() => 0);
        }

        public abstract Func<TValue, string, string> ToValue<TKey, TValue>(GTabSettings<TKey, TValue> settings) where TValue : Database.Tuple;
    }

    public enum PrintMode
    {
        WithAffixes,
        WithoutAffixes
    }

    public enum RelationalComparison
    {
        None,
        Le,
        Lt,
        Contains,
        Exclude,
        Eq,
        NotEq,
        Ge,
        Gt,
        And,
        Or,
        BinaryAnd,
        BinaryOr,
        BinaryLeftShift,
        BinaryRightShift,
        Add,
        Minus,
        Mult,
        Div,
        Mod,
        Pow,
        Not,
    }
}
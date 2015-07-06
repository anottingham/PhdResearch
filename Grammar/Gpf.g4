grammar Gpf;

@header {
using System;
}
program	
@init{ProtocolLibrary.Clear();}
		:	(p = protocol	{ProtocolLibrary.AddProtocol($p.value);} )+ main 
		;

main 
			:   'main' '(' ')' '{' 
			(	f = filter_def	{ ProtocolLibrary.Kernels.AddKernel($f.value);}
			|	i = int_def		{ ProtocolLibrary.Kernels.AddKernel($i.value);}
			)+ '}' {ProtocolLibrary.GenerateProgram();}
        ;

filter_def returns [FilterKernel value]
		:	'filter' i = ID '=' b = bool_expr ';' {$value = new FilterKernel($i.text, new Predicate($b.value));}
        ;

int_def returns [FieldKernel value]
		:	'int' p = ID '=' a = ID '.' b = ID ';' {$value = new FieldKernel($p.text, $a.text, $b.text);}
		;


protocol returns [Protocol value]
		:	'protocol' i = ID {$value = new Protocol($i.text);} ('[' l = integer']' {$value.DefaultLength = $l.value; })? 
				'{' (f = field[$value]  {$value.AddField($f.value);})+ (s = switch[$value] {$value.Switch = $s.value;})? '}' 
        ;

field[Protocol arg] returns [Field value]
		:	'field' i = ID '[' r = field_range ']' 
				{ 
					$value = new Field($i.text, $r.value, $arg); 
				} 
			( ';' 
			| 
			  '{'	
					( f = field_filter[$value]
						{
							$value.AddFilter($f.value);
						}
					)* 
					( s = statement 
						{
							$value.SetStatement(new Statement($arg, $s.value));
						}
					)?
			  '}' 
			)
		;

field_range returns [FieldRange value]
		:	a = integer ':' b = integer		
			{
				$value = new FieldRange($a.value, $b.value);
			}
		;


field_filter[Field arg] returns [FieldFilter value]
		:   '.' ID c = comparison i = integer ';' 
			{ 
				$value =  new FieldFilter($ID.text, $c.value, $i.value, $arg); 
			}
        ;

switch[Protocol arg]	returns [Switch value]
		:	'switch' '(' i = ID {$value = new Switch($i.text, $arg);} ')' '{' (s = switch_case {$value.AddCase($s.value);})+ '}' 
		;

switch_case returns [SwitchCase value]
		:   'case' a = ID ':' 'goto' b = ID ';' { $value = new SwitchCase($a.text, $b.text); }
        ;
		
statement returns [Expression value]
		:	'$length' '=' e = int_expr ';'	{ $value = new Expression($e.value); }
        ;

bool_expr returns [OrString value]
@init { $value = new OrString(); }
		:	a = bool_and  
			{
				$value.AddElement($a.value);
			} 
			(	
				'||' b = bool_and 
				{
					$value.AddElement($b.value);
				} 
			)+	
		;

bool_and returns [AndString value]
@init { $value = new AndString(); }
		:
			a = bool_not 
			{
				$value.AddElement($a.value);
			} 
			(	'&&' b = bool_not 
				{
					$value.AddElement($b.value);
				} 
			)+	
		;

bool_not returns [PredicateAtom value]
        :   '!' e = bool_atom	{ $value = new NotAtom($e.value); }
		|	e = bool_atom		{ $value = $e.value; }			
		;

bool_atom returns [PredicateAtom value]
		:	a = ID '.' b = ID '.' c = ID				{ $value = new FilterAtom($a.text, $b.text, $c.text); }
		|	i = ID										{ $value = new ProtocolAtom($i.text); }
        |   '(' e = bool_expr ')'						{ $value = new SubPredicateAtom($e.value); }
        |   a = ID '.' b = ID o = comparison i = INT	
			{ 
				//get the referenced field from the protocol library
				var tmp = ProtocolLibrary.GetField($a.text, $b.text);
				//construct an anonymous filter for this field
				var fieldFilter = new FieldFilter(MemoryCoordinator.GetAnonFilter(), $o.value, Convert.ToInt32($i.text), tmp);
				//add the filter to the field
				tmp.AddFilter(fieldFilter);
				//reference the newly created anonymous filter
				$value = new FilterAtom($a.text, $b.text, fieldFilter.ID); 
			}
		;

int_expr returns [AdditionString value]
@init { $value = new AdditionString(); }
		:	a = int_sub
			{
				$value.AddElement($a.value);
			} 
			( 
				'+' b = int_sub
				{ 
					$value.AddElement($b.value);
				} 
			)+	
        ;

int_sub returns [SubtractionString value]
@init { $value = new SubtractionString(); }
		:	a = int_mult 
			{
				$value.AddElement($a.value);
			} 
			( 
				'-' b = int_mult
				{ 
					$value.AddElement($b.value);
				} 
			)+	
        ;


		
int_mult returns [MultiplicationString value]
@init { $value = new MultiplicationString(); }
		:	a = int_div 
			{
				$value.AddElement($a.value);
			} 
			( 
				'*' b = int_div
				{ 
					$value.AddElement($b.value);
				} 
			)+	
        ;
		
		
int_div returns [DivisionString value]
@init { $value = new DivisionString(); }
		:	a = int_atom 
			{
				$value.AddElement($a.value);
			} 
			( 
				'/' b = int_atom
				{ 
					$value.AddElement($b.value);
				} 
			)+	
        ;
		
int_atom returns [ExpressionAtom value]
        :   r = system_register				{ $value = new RegisterAtom($r.value); }
		|	i = integer						{ $value = new StaticAtom($i.value); }
        |	'(' e = int_expr ')'			{ $value = new SubExpressionAtom($e.value);} 	
		;

comparison returns [Comparison value]
		:	'=='	{$value = Comparison.Equal;}
        |	'!='	{$value = Comparison.NotEqual;}
        |	'<'		{$value = Comparison.LessThan;}
        |	'>' 	{$value = Comparison.GreaterThan;}
        |	'<='	{$value = Comparison.LessThanOrEqual;}
        |	'>='	{$value = Comparison.GreaterThanOrEqual;}
        ;


system_register returns [SystemRegister value]
		:   '$length'		{$value = SystemRegister.Length;}
        |   '$value'		{$value = SystemRegister.Value;}				
        ;

integer returns [int value]
		:       a = INT '.' b = INT '.' c = INT '.' d = INT 
                        {
                            string msg = "";
                            int a = Convert.ToInt32($a.text);
                            int b = Convert.ToInt32($b.text);
                            int c = Convert.ToInt32($c.text);
                            int d = Convert.ToInt32($d.text);
                            
                            if ( a < 256 && b < 256 && c < 256 && d < 256)
                            {
                                $value = (a << 24) + (b << 16) + (c << 8) + d; 
                            }
                            else
                            {
                                msg += "Invalid IP address: " + a + '.' + b + '.' + c + '.' + d + "\n";
                                $value = 0;
                            }
                        }
                        //( need more infrastructure to change field comparison
                        //    '/' e = INT
                        //    {
                        //        int e = Convert.ToInt32($e.text);
                        //        if (e >= 8 && e <= 32) //ignore otherwise
                        //        {
                        //            $value &= (int)(0xFFFFFFFF << (32 - e));
                        //        }
                        //        else
                        //        {
                        //            msg += " Invalid Subnet Mask: " + "/" + e;
                        //        }
                        //    }
                        //)
                        {
                            if (msg != "") System.Windows.Forms.MessageBox.Show(msg, "Invalid IPv4 Address Specification");
                        }
		|	INT			{ $value = Convert.ToInt32($INT.text); }
		|	HEX			
			{
				string tmp = $HEX.text;
				tmp = tmp.Substring(2); //chop off leading '0x'
				int chars = tmp.Length;
				$value = 0;		
				for (int k = 0; k < chars; k++)
				{
					$value *= 16;
					switch(tmp[k])
					{
						case '0': break;
						case '1': $value += 1; break;
						case '2': $value += 2; break;
						case '3': $value += 3; break;
						case '4': $value += 4; break;
						case '5': $value += 5; break;
						case '6': $value += 6; break;
						case '7': $value += 7; break;
						case '8': $value += 8; break;
						case '9': $value += 9; break;
						case 'a': 
						case 'A': $value += 10; break;
						case 'b': 
						case 'B': $value += 11; break;
						case 'c': 
						case 'C': $value += 12; break;
						case 'd': 
						case 'D': $value += 13; break;
						case 'e': 
						case 'E': $value += 14; break;
						case 'f': 
						case 'F': $value += 15; break;
					}
				}		
			}

        ;

		
ID 		: 	('a'..'z'|'A'..'Z') ('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
        ;

INT 	: 	'0'..'9'+
        ;

HEX 	: 	'0' ('x'|'X') HexDigit+
		;

fragment HexDigit 
		:	('0'..'9'|'a'..'f'|'A'..'F')+
		;
 	
		
COMMENT	:   	
        (	'//' .*? '\n' 
    	|   '/*' .*? '*/'
        )	-> skip
    	;

WS  	:   ( ' '
        | 	'\t'
        | 	'\r'
        | 	'\n'
        )	-> skip
        ;
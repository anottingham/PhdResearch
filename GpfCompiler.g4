
grammar GpfCompiler;

program	:	protocol+ main
		;

main	:   'main' '(' ')' '{' ( filter_def | int_def )+ '}'
        ;

filter_def
		:	'filter' ID '=' expr ';'
        ;

int_def	:	'int' ID '=' expr ( '|' expr)* ';'
		;

protocol:	'protocol' ID '{' protocol_body+ '}'
        ;

protocol_body
		:	statement					#ProtocolStatement
        |	field						#ProtocolField
		|	decision					#ProtocolDecision
		|	switch						#ProtocolSwitch
		;

statement
		:	var_type ID '=' expr ';'	#DeclareAssign
		|	var_type ID ';'				#Declare
        |   ID '=' expr ';'				#Assign
        ;

switch	:	'switch' '(' ID ')' '{' switch_case+ '}'
		;

switch_case
		:   'case' ID ':' switch_body
        ;

switch_body
		:   ( statement )* goto
        ;

goto	:   'goto' ID ';'
		;

decision:	'if' '(' ID ')' '{' if_body '}'
        ;

if_body	:	field+						#IfField
		|	switch_body					#IfSwitch
		;

field	:	field_decl ';'					#FieldEmpty
		|	field_decl '{' field_body '}'	#FieldFull
		;

field_decl
		:	'field' ID '[' field_range ']' 
		;

field_range
		:	integer ':' integer			#RangeFull
		|	':' integer					#RangeImplicit
		;

field_body
		:   statement					#FieldStatement
        |   field_filter				#FieldFilter
        ;

field_filter
		:   FF_ID comparison_op integer ';'
        ;

expr    :   expr '||' expr				#ExprOr
        |   expr '&&' expr				#ExprAnd
        |   '!' expr					#ExprNot
        |   expr comparison_op expr		#ExprCompare
        |   expr ( '*' | '/' ) expr		#ExprMult
        |   expr ( '+' | '-' ) expr		#ExprSum
        |   '(' expr ')'				#ExprParen
        |   system_register				#ExprSysReg
        |   ID							#ExprID
        |	INT							#ExprInt
        ;

comparison_op
		:	'==' 
        |	'!='
        |	'<'
        |	'>'
        |	'<='
        |	'>='
        ;

var_type:	'int'						#TypeInt
		|	'bool'						#TypeBool
        ;

system_register
		:   '$length'					#SysRegLen
        |   '$value'					#SysRegVal
        ;

integer :   INT							#ValueInt
		|	HEX							#ValueHex
        ;



FF_ID   :   '.' ('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
        ;   //field filter ID

ID 		: 	('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'_'|'0'..'9')*
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
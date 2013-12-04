#include <avr/io.h>
#define F_CPU 8000000UL
#include <avr/interrupt.h>
#include <util/delay.h>

// ********** Disposable functions **********************************
// ********** LCD Port Information (from microcontroller) ***********
#define	LCD_OUT		PORTA
#define	LCD_IN		PINA
#define	LCD_DDR		DDRA
#define	ENABLE		6
#define	RW			5
#define	RS			4
#define	D7			3
#define	D6			2
#define	D5			1
#define	D4			0

//	Register Select Constants
#define	DATA_REGISTER		0
#define	COMMAND_REGISTER	1

volatile static unsigned int timeCount;
volatile int position;

void initDelay(void);
void delay50us(unsigned int);
void waitForLCD(void);
void initLCD(void);
void writeNibbleToLCD(int, int);
unsigned char BV(unsigned char);
void writeByteToLCD(int, int);

void initDelay(){
	TCCR0 = 0x02;
	TCNT0 = 212;
	OCR0 = 0x00;
	TIMSK = 0x01;

	sei();
}

void delay50us(unsigned int delayedTime){
	//this exactly gives 50us delay for 8MHz systemClock
	TCNT0 = 212;
	timeCount = 0;
	while(1){
		if (timeCount == delayedTime) {
			break;
		}
	}
}

void waitForLCD(){
	delay50us(1);			//wait for lcd to write data;
}

void initLCD(){
	position = 0;
	LCD_DDR = 0x7F;

	delay50us(300);			//waiting 15ms for LCD to power up

	writeNibbleToLCD(COMMAND_REGISTER, 0x03);
	delay50us(100);
	writeNibbleToLCD(COMMAND_REGISTER, 0x03);
	delay50us(100);
	writeNibbleToLCD(COMMAND_REGISTER, 0x03);
	delay50us(100);
	writeNibbleToLCD(COMMAND_REGISTER, 0x02);
	delay50us(100);
	
	// ************** Function set ******************************
	writeByteToLCD(COMMAND_REGISTER, 0x28);
	delay50us(250);
	// **********************************************************

	// ************* Turn display off ***************************
	writeByteToLCD(COMMAND_REGISTER, 0x08);
	delay50us(250);
	// **********************************************************
	
	// ************* Clear LCD and return home ******************
	writeByteToLCD(COMMAND_REGISTER, 0x01);
	delay50us(250);
	// **********************************************************
	
	// ************* Turn on display, turn on cursor and blink **
	writeByteToLCD(COMMAND_REGISTER, 0x0E);
	delay50us(250);
	// **********************************************************
}

void writeNibbleToLCD(int selectedRegister, int nibble) {
	LCD_OUT = BV(ENABLE);

	LCD_OUT |= nibble;

	if(selectedRegister == DATA_REGISTER) {
		LCD_OUT |= BV(RS);
	} else {
		LCD_OUT &= ~BV(RS);
	}
	
	LCD_OUT &= ~BV(ENABLE);
}

unsigned char BV(unsigned char commValue){
	unsigned char a;
	a = 1 << commValue;
	return a;
}

void writeByteToLCD(int selectedRegister, int byte) {
	int upperNibble = byte >> 4;
	int lowerNibble = byte & 0x0f;
	
	if(selectedRegister == DATA_REGISTER && position == 16) {
		writeByteToLCD(COMMAND_REGISTER, 0xC0);
		delay50us(25);
	}else if(selectedRegister == DATA_REGISTER && position == 32) {
		writeByteToLCD(COMMAND_REGISTER, 0x80);
		delay50us(25);
	}
	
	waitForLCD();
	writeNibbleToLCD(selectedRegister, upperNibble);
	waitForLCD();
	writeNibbleToLCD(selectedRegister, lowerNibble);

	if(selectedRegister == DATA_REGISTER && ++position == 33)	
		position = 1;
}

void clearLCD(void) {
	writeByteToLCD(COMMAND_REGISTER, 0x01);
	position = 0;
	delay50us(250);
}

void writeStringToLCD (char stringValue[]){
	int x=0;
	while (stringValue[x] != '\0'){
		writeByteToLCD (DATA_REGISTER, stringValue[x]);
		x++;
	}
}

ISR (TIMER0_OVF_vect){
	TCNT0 = 212;
	++timeCount;
}
// *********************************************************************

void USART_Init( unsigned char ubrr) {
	UBRRH = 0;
	UBRRL = ubrr;


	UCSRB|= (1<<RXEN)|(1<<TXEN);

	UCSRC |= (1 << URSEL)|(3<<UCSZ0);
}

void USART_Transmit( unsigned char data ) {

	while ( !( UCSRA & (1<<UDRE)) );
	UDR = data;

}

void USART_Transmit_String(char data[]){
	for (int x=0; ; ){
		if (data[x] != '\0') {
			USART_Transmit (data[x]);
		} else {
			break;
		}
		x += 1;
		_delay_ms(50);
	}
}

unsigned char USART_Receive( void ) {

	while ( !(UCSRA & (1<<RXC)) );

	return UDR;
}

int stringCompare (char a[], char b[]) {
	int flag = 1;
	int x;
	x = 0;
	do {
		if (a[x] != b[x]) {
			flag = 0;
			break;
		}
		if (a[x] == '\0') {
			break;
		}
		x ++;
	} while (1);
	return flag;
}

int main(){
	char a[50], b[50];
	int x;
	char valPortB;

	DDRB = 0xFF;
	PORTB = 0x00;
	valPortB = 0x00;
	DDRC = 0xFF;

	initDelay();
	initLCD();
	USART_Init (51);

	writeStringToLCD("Home automation + speech recoz");
	do {
	
		x = 0;
		do {
			a[x] = USART_Receive();
		
			if (a[x-3]=='V' && a[x-2]=='E' && a[x-1]=='N' && a[x]=='D') {
				break;
			}	

			x += 1;
		} while(1);
		a[x-4] = '\0';

		x = 2;
		do {
			if (a[x] != '\0') {
				b[x-2] = a[x];
			} else {
				break;
			}
			x ++;
		} while (1);
		b[x-2] = '\0';


		clearLCD();
		if (stringCompare (b, "deviceoneon")){
			valPortB |= 0x01;
			PORTB |= 0x01;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device One ONed");
		} else if (stringCompare (b, "deviceoneoff")) {
			valPortB &= 0xFE;
			PORTB &= 0xFE;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device One OFFed");	
		} else if (stringCompare (b, "devicetwoon")) {
			valPortB |= 0x02;
			PORTB |= 0x02;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device Two ONed");
		} else if (stringCompare (b, "devicetwooff")) {
			valPortB &= 0xFD;
			PORTB &= 0xFD;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device Two OFFed");
		} else if (stringCompare (b, "devicethreeon")) {
			valPortB |= 0x04;
			PORTB |= 0x04;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device Three ON");
		} else if (stringCompare (b, "devicethreeoff")) {
			valPortB &= 0xFB;
			PORTB &= 0xFB;
			USART_Transmit_String("U_setOK_VEND");
			writeStringToLCD ("status:         ");
			writeStringToLCD ("Device Three OFF");
		} else if (stringCompare (b, "statusdevices")) {
			writeStringToLCD ("status:         ");
			USART_Transmit_String ("U_");
			USART_Transmit (PORTB + 0x30);
			_delay_ms(5);
			USART_Transmit_String("_VEND");
			writeStringToLCD ("successful");
		} else {
			writeStringToLCD ("status:         ");
			USART_Transmit_String("U_setCANCEL_VEND");
			writeStringToLCD ("unrecognized key");
		}
	} while(1);

	return 0;
}

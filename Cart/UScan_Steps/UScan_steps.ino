/*************************************************************************************/
/* Ultrascanguide robot motor controller    2015 - 2020                              */
/* contributors: Rita Kambil,  Austin Poteet, Hang Shu, Ted Selker                   */
/* Stepper motor steps are used as  distance, early versions used an optical encoder */
/* pin 56 stepper, 9,10 limit switches, 11 stop button 12 possible speaker           */
/* 1oop calls state nmachine, calls stepper driver   to setup, run and change stuff  */
/* find limits and count steps to tell SDK scan lengths                              */
/*wait for commands forward, reverse, scan                                           */
/*************************************************************************************/

#include "PinChangeInt.h"
#include "Arduino.h"
#include "BasicStepperDriver.h" // generic


// useful for sanity checks
// digitalWrite(13, 1);   // turn the LED on (HIGH is the voltage level)
// delay(500);                       // wait for a second
// digitalWrite(13, 0);    // turn the LED off by making the voltage LOW
// delay(500);
// sprintf(buf, "limit is %d pwm is %d", digitalRead(limit_1), pwm_val);
// Serial.println(buf);

#define DEBUG 0
#define MOTOR_STEPS 200
#define MIN_RPM 120
#define RPM 120
#define RPM_SCANNING 30
#define MICROSTEPS 1
#define LARGE_NUM 200000

enum my_states {DISCONNECTED, UNINITIALIZED, HOMING, READY, SCANNING, ENDOFTRAVEL, REWINDING, STOPPED};
my_states state = DISCONNECTED; //initially disconnected

// IMPORTANT NOTE: left and right refer to when you're facing the back of the probe looking into the circular rim
// constants won't change. They're used here to
// set pin numbers:
const int stop_start = 11;  // the number of the pushbutton pin //9
const int manual_fwd = 2;   // manual forward switch
const int manual_bwd = 3;   // manual backward switch
const int enable_pin = 4;
const int dir_pin = 5;			// stepper direction
const int step_pin = 6;			// stepper step  
const int opt_encoder = 8;  // optical encoder pin                                /*currently unused feature */
const int limit_1 = 9;      // limit switch 1 blue (the switch on the left)
const int limit_2 = 10;     // limit switch 2 red (the switch on the right)
const int speaker = 12;                                                          /*currently unused feature */

int write_flag = 0;         // tells Arduino to call Serial.write() send information to ArraySDK; if  1 
int motor_flag = 0;         
int home_flag_1 = 0;        // if set to 1,  robot has hit the right limit
int home_flag_2 = 0;        // if set to 1, robot has hit the left limit
int led = 0;
int step_count = 0;
int rpm = RPM;              //maximium rpm for homing,  scan is RPM/4
char buf[100];

static unsigned long last_interrupt_time = 0;

BasicStepperDriver stepper(MOTOR_STEPS, dir_pin, step_pin, enable_pin);

void serialEvent() {
  while (Serial.available()) {
    // get the new byte:
    char inChar = (char)Serial.read();
    byte bChar = (byte) inChar;

    //need to figure out new math for stepper speed
    if ((bChar & 0x40) == 0) { 	//if bit 7 is set this is a speed command
      int speed = (int)(bChar & 0x3F);
      rpm = (speed * 2) + MIN_RPM;
      stepper.setRPM(rpm);
    }
    switch (inChar) {
      case ('m'): //start robot
        state = SCANNING;
        //Serial.write(0x4D);
        motor_flag = 1;
        break;
      case ('n'): //rewind robot
        state = REWINDING;
        motor_flag = 1;
        break;
      case ('D'): //initial query state
        //go_home();
        //toggle_led();
        rpm = MIN_RPM;
        home_flag_1 = 0;
        home_flag_2 = 0;
        state = HOMING; //want to rewind before doing anything
        motor_flag = 1;
        Serial.write(0x40);
        break;
      case ('l'): //stop robot
        write_flag = 1;
        state = STOPPED;
        Serial.write(0x47);
        break;
    }
    return;
  }
}

void toggle_led() {
  bool led = digitalRead(13);
  digitalWrite(13, !led);
  return;
}

//triggered on limit switch, switches the state of the motor
void interrupt_limit_switch() {
  unsigned long interrupt_time = millis();
  if (interrupt_time - last_interrupt_time > 200) {
    //when limit switch is hit, stop and be ready to reverse direction
    if (state == SCANNING) {
      state = ENDOFTRAVEL;
      write_flag = 1;
    }
    else if (state == REWINDING) {
      state = READY;
      write_flag = 1;
    }
    else if (state == HOMING) {
      if (!home_flag_1) {
        home_flag_1 = 1;
        motor_flag = 1;
        write_flag = 1;
      }
      else { //home flag 1 is already set, so set home flag 2
        home_flag_2 = 1;
        //motor_flag = 1; //going to be stopping motor
        write_flag = 1;
      }
    }
  }
  last_interrupt_time = interrupt_time;
  return;
}
//e-stop button
void interrupt_start_stop() {
  unsigned long interrupt_time = millis();
  if (interrupt_time - last_interrupt_time > 200) {
    state = STOPPED;
    write_flag = 1;
  }
  last_interrupt_time = interrupt_time;
  return;
}

void ping() {
  for (int i = 0; i < 100; i++) {
    digitalWrite(12, 0);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 32);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 64);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 128);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 255);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 128);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 64);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 32);   // turn the LED on (HIGH is the voltage level)
    digitalWrite(12, 0);   // turn the LED on (HIGH is the voltage level)
  }
}

void state_machine() {
  switch (state) {
    case DISCONNECTED:
      //nothing here
      break;
    case HOMING:
      if (!home_flag_1 && !home_flag_2 && motor_flag) { //rewind, and don't count steps
        toggle_led();
        stepper.enable();
        // stepper.setRPM(80);
        stepper.setRPM(RPM);
        stepper.startMove(-1 * LARGE_NUM);	//start motor rewinding
        motor_flag = 0;
      }
      else if (digitalRead(limit_2)) { //if already rewound
        home_flag_1 = 1;
        write_flag = 1;
        motor_flag = 1;
        
        //        stepper.stop();
        //        stepper.disable();
      }
      if (home_flag_1 && !home_flag_2 && write_flag && motor_flag) { // Send byte saying listen for steps (counting steps, used for maxSteps in ArraySDK), go until EOT
        toggle_led();
        stepper.stop();
        Serial.write(0x42);
        write_flag = 0;
        stepper.startMove(LARGE_NUM);
        motor_flag = 0;
      }
      if (home_flag_1 && home_flag_2 && write_flag) { // send finish counting steps byte, switch to rewind state
        toggle_led();
        stepper.stop();
        Serial.write(0x41);
        write_flag = 0;
        state = REWINDING;
        motor_flag = 1;
      }
      break;
    case READY:
      if (write_flag) {
        toggle_led();
        stepper.stop();
        stepper.disable();
        step_count = 0;
        Serial.write(0x4D);
        write_flag = 0;
      }
      break;
    case SCANNING:
      if (motor_flag) {  						// start motor forward
        stepper.enable();
        // stepper.setRPM(rpm);
        stepper.setRPM(RPM_SCANNING);
        stepper.startMove(LARGE_NUM);
        motor_flag = 0;
      }
      if (digitalRead(limit_1)) {
        state = ENDOFTRAVEL;
        write_flag = 1;
      }
      break;
    case REWINDING:
      if (motor_flag) {
        stepper.enable();
        stepper.setRPM(120);
        stepper.startMove(-1 * LARGE_NUM);	//start motor rewinding
        motor_flag = 0;
      }
      if (digitalRead(limit_2)) {
        state = READY;
        write_flag = 1;
      }
      break;
    case ENDOFTRAVEL:
      if (write_flag || digitalRead(limit_2)) {
        stepper.stop();						//stop motor
        stepper.disable();
        Serial.write(0x4B);					//send byte
        write_flag = 0;
      }
      break;
    case STOPPED:
      if (write_flag) {
        stepper.stop(); 					//stop motor
        stepper.disable();
        Serial.write(0x47); 				//send byte saying stopped
        write_flag = 0;
      }
      if (digitalRead(limit_2)) {
        state = READY;
        write_flag = 1;
      }
      break;
  }
}

void setup() {
  //setup buttons
  Serial.begin(38400);
  pinMode(stop_start, INPUT);
  pinMode(manual_fwd, INPUT);
  pinMode(manual_bwd, INPUT);
  pinMode(opt_encoder, INPUT);
  pinMode(limit_1, INPUT);
  pinMode(limit_2, INPUT);

  pinMode(13, OUTPUT); //led
  pinMode(dir_pin, OUTPUT);
  pinMode(step_pin, OUTPUT);
  pinMode(speaker, OUTPUT);

  attachPinChangeInterrupt(stop_start, interrupt_start_stop, RISING);
  attachPinChangeInterrupt(limit_1, interrupt_limit_switch, RISING);
  attachPinChangeInterrupt(limit_2, interrupt_limit_switch, RISING);
  stepper.begin(RPM, MICROSTEPS);
}

void loop() {
  // motor control via state machine
  state_machine();

  unsigned wait_time_micros = stepper.nextAction();
  // 0 wait time indicates the motor has stopped
  if (wait_time_micros <= 0) {
    stepper.disable();       // comment out to keep motor powered
  }
  // (optional) execute other code if we have enough time
  if (wait_time_micros > 100) {
      //    	if (state == REWINDING){ step_count--;};
      //    	if (state == SCANNING || HOMING) { step_count++;};
      step_count++;
      
      if (step_count % 10 == 0 && ((state == SCANNING) || (state == REWINDING))) {
        toggle_led();
        Serial.write(0x50); //tell computer we took a step (1/2 rotation)
      }
  }

  /*
    if (state == HOMING) {
    Serial.print("home_flag_1: ");
    Serial.println(home_flag_1);
    Serial.print("home_flag_2: ");
    Serial.println(home_flag_2);
    Serial.print("write_flag: " + write_flag);
    Serial.println(write_flag);
    Serial.print("motor_flag: " + motor_flag);
    Serial.println(motor_flag);
    }
  */

  /*
  if (state == 2 && state == HOMING) {
    Serial.print(state);
  }
   */
}

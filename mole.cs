/*
Mole.cs is intended to control a pistons based miner
*/

// PI 3.14
const float PI = (float)Math.PI;

// Extending arms params
// Low speed and little force in order to contact wall gently
const float armsPistonsExtendSpeed = 0.2f;
const float armsPistonsExtendMaxImpulseAxis = 10000;
const float armsPistonsExtendMaxImpulseNonAxis = 10000; 

// Retract arms params
// High speed and middle force in order to free arms plate from wall glitch
const float armsPistonsRetractSpeed = 2.0f;
const float armsPistonsRetractMaxImpulseAxis = 70000;
const float armsPistonsRetractMaxImpulseNonAxis = 70000;

// Extending vertical pistons params
// Low speed and middle force in order to mine
const float vPistonsExpandSpeed = 0.1f;
const float vPistonsExpandMaxImpulseAxis = 70000;
const float vPistonsExpandMaxImpulseNonAxis = 70000; 

// Retracting vertical pistons params
// High speed and middle force in order retract
const float vPistonsRetractSpeed = 1.5f;
const float vPistonsRetractSpeedDrillMotorBlocked = 0.2f;
const float vPistonsRetractMaxImpulseAxis = 70000;
const float vPistonsRetractMaxImpulseNonAxis = 70000; 

// Extending vertical pistons params
// Middle speed and very high force in order push up heavy loaded machine
// Only for LoopUp
const float vPistonsLoopUpExpandSpeed = 1.0f;
const float vPistonsLoopUpExpandMaxImpulseAxis = 100000000000;
const float vPistonsLoopUpExpandMaxImpulseNonAxis = 100000000000;

// Drills motor speed
const float drillRotorSpeed = 1.5f;
// Minimum angular rotation per tick before considering motor as blocked
const float minimumDrillRotorRotationPerExecution = 0.5f;

// Minimum arms to be locked
const int minimumArmLocked = 4;


const string verticalPushPistonTag = "piston_v";

const string armsUpPistonTag = "piston_h_up";
const string armsUpMagneticPlateTag = "plate_h_up";

const string armsDownPistonTag = "piston_h_down";
const string armsDownMagneticPlateTag = "plate_h_down";

const string drillMotorTag = "drill_motor";

const string drillTag = "drill";


List<IMyPistonBase> verticalPushPistons;

List<IMyPistonBase> armsUpPistons;
List<IMyLandingGear> armsUpMagneticPlates;

List<IMyPistonBase> armsDownPistons;
List<IMyLandingGear> armsDownMagneticPlates;

IMyMotorStator drillMotor;

List<IMyShipDrill> drills;

IMyTextPanel lcdPrimary;
IMyTextPanel lcdSecondary;
IMyTextPanel lcdTertiary;


string text;
string textLcd2;
string textLcd3;
bool inverseSketchPart = true;

float lastDrillMotorAngle = 0.0f;
bool isDrillMotorBlocked = false;

MoleStatus currentStatus = MoleStatus.Stop;
MoleSteps currentStep = MoleSteps.Pending;
Orders currentOrder = Orders.Stop;
MoleErrorSteps currentErrorStep = MoleErrorSteps.None;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update10;

    this.verticalPushPistons = this.GetBlocksContainingName<IMyPistonBase>(verticalPushPistonTag);

    this.armsUpPistons = this.GetBlocksContainingName<IMyPistonBase>(armsUpPistonTag);
    this.armsUpMagneticPlates = this.GetBlocksContainingName<IMyLandingGear>(armsUpMagneticPlateTag);

    this.armsDownPistons = this.GetBlocksContainingName<IMyPistonBase>(armsDownPistonTag);
    this.armsDownMagneticPlates = this.GetBlocksContainingName<IMyLandingGear>(armsDownMagneticPlateTag);

    this.drillMotor = this.GetBlocksContainingName<IMyMotorStator>(drillMotorTag)[0];

    this.drills = this.GetBlocksContainingName<IMyShipDrill>(drillTag);

    List<IMyTextPanel> lcdsPrimary = this.GetBlocksContainingName<IMyTextPanel>("lcd_primary");
    List<IMyTextPanel> lcdsSecondary = this.GetBlocksContainingName<IMyTextPanel>("lcd_secondary");
    List<IMyTextPanel> lcdsTertiary = this.GetBlocksContainingName<IMyTextPanel>("lcd_tertiary");

    if (lcdsPrimary.Count > 0)
    {
        this.lcdPrimary = lcdsPrimary[0];
    }

    if (lcdsSecondary.Count > 0)
    {
        this.lcdSecondary = lcdsSecondary[0];
    }

    if (lcdsTertiary.Count > 0)
    {
        this.lcdTertiary = lcdsTertiary[0];
    }

    this.lcdPrimary.ContentType = ContentType.TEXT_AND_IMAGE;
    this.lcdPrimary.FontSize = 0.7f;
    this.lcdPrimary.SetValue<long>("Font", 1147350002); // Monospace
    this.lcdPrimary.SetValue("FontColor", new Color(20, 160, 0, 255));
    this.lcdPrimary.SetValue("BackgroundColor", new Color(6, 20, 2, 255));

    this.lcdSecondary.ContentType = ContentType.TEXT_AND_IMAGE;
    this.lcdSecondary.FontSize = 0.7f;
    this.lcdSecondary.SetValue<long>("Font", 1147350002); // Monospace
    this.lcdSecondary.SetValue("FontColor", new Color(20, 160, 0, 255));
    this.lcdSecondary.SetValue("BackgroundColor", new Color(6, 20, 2, 255));

    this.lcdTertiary.ContentType = ContentType.TEXT_AND_IMAGE;
    this.lcdTertiary.FontSize = 0.7f;
    this.lcdTertiary.SetValue<long>("Font", 1147350002); // Monospace
    this.lcdTertiary.SetValue("FontColor", new Color(20, 160, 0, 255));
    this.lcdTertiary.SetValue("BackgroundColor", new Color(6, 20, 2, 255));
}

public void Save()
{
}

/// <summary>
/// Called every x ticks (see Program())
/// </summary>
public void Main(string argument, UpdateType updateSource)
{
    switch (argument)
    {
        case "run_down":
        {
            if (this.currentOrder != Orders.RunDown)
            {
                this.currentOrder = Orders.RunDown;
                this.currentStep = MoleSteps.Init;
            }
            break;
        }
        case "run_up":
        {
            if (this.currentOrder != Orders.RunUp)
            {
                this.currentOrder = Orders.RunUp;
                this.currentStep = MoleSteps.Init;
            }
            break;
        }
        case "stop":
        {
            this.currentOrder = Orders.Stop;
            this.currentStep = MoleSteps.Pending;
            break;
        }
        default:
            break;
    }

    // Check if drill motor is blocked
    this.IsDrillMotorBlocked();

    switch (this.currentOrder)
    {
        case Orders.Stop:
        {
            this.currentStatus = MoleStatus.Stop;
            this.Stop();
            break;
        }
        case Orders.RunUp:
        {
            this.currentStatus = MoleStatus.LoopUp;
            this.LoopUp();
            break;
        }
        case Orders.RunDown:
        {
            this.currentStatus = MoleStatus.LoopDown;
            this.LoopDown();
            break;
        }
        default:
            break;
    }
    
    // Prepare text to be drawed on LCDs
    this.DrawOnScreen("");

    // Draw
    this.Display();
}

/// <summary>
/// Stop machine
/// </summary>
void Stop()
{
    for (int i = 0; i < this.verticalPushPistons.Count; ++i)
    {
        this.SetPistonMovement(this.verticalPushPistons[i], PistonMovement.Stop, 0.0f, 0.0f, 0.0f);
    }
    this.drillMotor.ApplyAction("OnOff_Off");
    for (int i = 0; i < this.drills.Count; ++i)
    {
        this.drills[i].ApplyAction("OnOff_Off");
    }
}

/// <summary>
/// Loop of actions to move machine down (and mine)
/// </summary>
void LoopDown()
{
    switch(this.currentStep)
    {
        case MoleSteps.Init:
        {
            for (int i = 0; i < this.verticalPushPistons.Count; ++i)
            {
                this.SetPistonMovement(this.verticalPushPistons[i], PistonMovement.Stop, 0.0f, 0.0f, 0.0f);
            }
            for (int i = 0; i < this.drills.Count; ++i)
            {
                this.drills[i].ApplyAction("OnOff_On");
            }
            this.drillMotor.ApplyAction("OnOff_On");
            if (this.CheckMagneticPlates(EArms.UpArms))
            {
                if (this.CheckPistonsArmsFullyRetracted(EArms.DownArms))
                {
                    this.currentStep = MoleSteps.ExtendVPush;
                }
                else
                {
                    this.RetractArms(EArms.DownArms);
                }
            }
            else
            {
                this.ExtendArms(EArms.UpArms);
            }
            break;
        }
        case MoleSteps.ExtendVPush:
        {
            if (this.CheckPistonsVPushForPosition(10.0f))
            {
                this.currentStep = MoleSteps.LockDownArms;
            }
            else
            {
                if (this.isDrillMotorBlocked) 
                {
                    this.currentErrorStep = MoleErrorSteps.DrillMotorBlocked;
                    this.RetractVPushPiston(vPistonsRetractSpeedDrillMotorBlocked, vPistonsRetractMaxImpulseAxis, vPistonsRetractMaxImpulseNonAxis);
                    this.VPistonHeightCorrection();
                }
                else
                {
                    this.currentErrorStep = MoleErrorSteps.None;
                    this.ExtendVPushPiston(vPistonsExpandSpeed, vPistonsExpandMaxImpulseAxis, vPistonsExpandMaxImpulseNonAxis);
                    this.VPistonHeightCorrection();
                }
            }
            break;
        }
        case MoleSteps.LockDownArms:
        {
            if (this.CheckMagneticPlates(EArms.DownArms))
            {
                this.currentStep = MoleSteps.UnlockUpArms;
            }
            else
            {
                this.ExtendArms(EArms.DownArms);
            }
            break;
        }
        case MoleSteps.UnlockUpArms:
        {
            if (this.CheckPistonsArmsFullyRetracted(EArms.UpArms))
            {
                this.currentStep = MoleSteps.RetractVPush;
            }
            else
            {
                this.RetractArms(EArms.UpArms);
            }
            break;
        }
        case MoleSteps.RetractVPush:
        {
            if (this.CheckPistonsVPushFullyRetracted())
            {
                this.currentStep = MoleSteps.LockUpArms;
            }
            else
            {
                this.RetractVPushPiston(vPistonsRetractSpeed, vPistonsRetractMaxImpulseAxis, vPistonsRetractMaxImpulseNonAxis);
                this.VPistonHeightCorrection();
            }
            break;
        }
        case MoleSteps.LockUpArms:
        {
            if (this.CheckMagneticPlates(EArms.UpArms))
            {
                this.currentStep = MoleSteps.UnlockDownArms;
            }
            else
            {
                this.ExtendArms(EArms.UpArms);
            }
            break;
        }
        case MoleSteps.UnlockDownArms:
        {
            if (this.CheckPistonsArmsFullyRetracted(EArms.DownArms))
            {
                this.currentStep = MoleSteps.ExtendVPush;
            }
            else
            {
                this.RetractArms(EArms.DownArms);
            }
            break;
        }
        default:
            break;
    }
}

/// <summary>
/// Loop of actions to move machine up
/// </summary>
void LoopUp()
{
    switch(this.currentStep)
    {
        case MoleSteps.Init:
        {
            for (int i = 0; i < this.verticalPushPistons.Count; ++i)
            {
                this.SetPistonMovement(this.verticalPushPistons[i], PistonMovement.Stop, 0.0f, 0.0f, 0.0f);
            }
            this.drillMotor.ApplyAction("OnOff_Off");
            for (int i = 0; i < this.drills.Count; ++i)
            {
                this.drills[i].ApplyAction("OnOff_Off");
            }
            if (this.CheckMagneticPlates(EArms.DownArms))
            {
                if (this.CheckPistonsArmsFullyRetracted(EArms.UpArms))
                {
                    this.currentStep = MoleSteps.ExtendVPush;
                }
                else
                {
                    this.RetractArms(EArms.UpArms);
                }
            }
            else
            {
                this.ExtendArms(EArms.DownArms);
            }
            break;
        }
        case MoleSteps.ExtendVPush:
        {
            if (this.CheckPistonsVPushForPosition(10.0f))
            {
                this.currentStep = MoleSteps.LockUpArms;
            }
            else
            {
                this.ExtendVPushPiston(vPistonsLoopUpExpandSpeed, vPistonsLoopUpExpandMaxImpulseAxis, vPistonsLoopUpExpandMaxImpulseNonAxis);
                this.VPistonHeightCorrection();
            }
            break;
        }
        case MoleSteps.LockDownArms:
        {
            if (this.CheckMagneticPlates(EArms.DownArms))
            {
                this.currentStep = MoleSteps.UnlockUpArms;
            }
            else
            {
                this.ExtendArms(EArms.DownArms);
            }
            break;
        }
        case MoleSteps.UnlockUpArms:
        {
            if (this.CheckPistonsArmsFullyRetracted(EArms.UpArms))
            {
                this.currentStep = MoleSteps.ExtendVPush;
            }
            else
            {
                this.RetractArms(EArms.UpArms);
            }
            break;
        }
        case MoleSteps.RetractVPush:
        {
            if (this.CheckPistonsVPushFullyRetracted())
            {
                this.currentStep = MoleSteps.LockDownArms;
            }
            else
            {
                this.RetractVPushPiston(vPistonsRetractSpeed, vPistonsRetractMaxImpulseAxis, vPistonsRetractMaxImpulseNonAxis);
                this.VPistonHeightCorrection();
            }
            break;
        }
        case MoleSteps.LockUpArms:
        {
            if (this.CheckMagneticPlates(EArms.UpArms))
            {
                this.currentStep = MoleSteps.UnlockDownArms;
            }
            else
            {
                this.ExtendArms(EArms.UpArms);
            }
            break;
        }
        case MoleSteps.UnlockDownArms:
        {
            if (this.CheckPistonsArmsFullyRetracted(EArms.DownArms))
            {
                this.currentStep = MoleSteps.RetractVPush;
            }
            else
            {
                this.RetractArms(EArms.DownArms);
            }
            break;
        }
        default:
            break;
    }
}

/// <summary>
/// Get all blocks of type <T> from this grid where name contain string
/// </summary>
/// <param name="name"> Name that must be found in block's name </param>
/// <returns>Blocks of type <T></returns>
List<T> GetBlocksContainingName<T>(string name) where T : class
{
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    List<T> output = new List<T>();
    GridTerminalSystem.GetBlocksOfType<T>(blocks);
    for (int i = 0; i < blocks.Count; i++) {
        if (blocks[i].CustomName.ToString().Contains(name)) 
        {
            output.Add(blocks[i] as T);
        }
    }
    return output;
}



void ExtendVPushPiston(float speed, float maxImpulseAxis, float maxImpulseNonAxis)
{
    for (int i = 0; i < this.verticalPushPistons.Count; ++i) {
        this.SetPistonMovement(this.verticalPushPistons[i], PistonMovement.Extend, speed, maxImpulseAxis, maxImpulseNonAxis);
        // this.verticalPushPistons[i].SetValue<float>("Velocity", speed);
        // this.verticalPushPistons[i].SetValue<float>("MaxImpulseAxis", maxImpulseAxis);
        // this.verticalPushPistons[i].SetValue<float>("MaxImpulseAxis", maxImpulseNonAxis);
        // this.verticalPushPistons[i].Extend();
    }
}

void RetractVPushPiston(float speed, float maxImpulseAxis, float maxImpulseNonAxis)
{
    for (int i = 0; i < this.verticalPushPistons.Count; ++i) {
        this.SetPistonMovement(this.verticalPushPistons[i], PistonMovement.Retract, speed, maxImpulseAxis, maxImpulseNonAxis);
        // .SetValue<float>("Velocity", speed);
        // this.verticalPushPistons[i].SetValue<float>("MaxImpulseAxis", maxImpulseAxis);
        // this.verticalPushPistons[i].SetValue<float>("MaxImpulseAxis", maxImpulseNonAxis);
        // this.verticalPushPistons[i].Retract();
    }
}

/// <summary>
/// Move piston according to parameters
/// </summary>
/// <param name="piston"> Piston to move </param>
/// <param name="movement"> Type of movement </param>
/// <param name="speed"> Speed of movement (meters/second)</param>
/// <param name="maxImpulseAxis"> Force in axis (Newton Meter)</param>
/// <param name="maxImpulseNonAxis"> Force out of axis (Newton Meter)</param>
/// <returns>void</returns>
void SetPistonMovement(IMyPistonBase piston, PistonMovement movement, float speed, float maxImpulseAxis, float maxImpulseNonAxis)
{
    // Set piston params
    piston.SetValue<float>("Velocity", speed);
    piston.SetValue<float>("MaxImpulseAxis", maxImpulseAxis);
    piston.SetValue<float>("MaxImpulseAxis", maxImpulseNonAxis);

    // Set piston movement
    if (movement == PistonMovement.Stop) {
        piston.SetValue<float>("Velocity", 0.0f);
    } else if (movement == PistonMovement.Extend) {
        piston.Extend();
    } else if (movement == PistonMovement.Retract) {
        piston.Retract();
    }
}

void RetractArms(EArms arms)
{
    switch (arms)
    {
        case EArms.UpArms: 
        {
            for (int i = 0; i < this.armsUpMagneticPlates.Count; ++i) {
                this.armsUpMagneticPlates[i].AutoLock = false;
                this.armsUpMagneticPlates[i].Unlock();
            }
            
            for (int i = 0; i < this.armsUpPistons.Count; ++i) {
                this.SetPistonMovement(
                    this.armsUpPistons[i],
                    PistonMovement.Retract,
                    armsPistonsRetractSpeed,
                    armsPistonsRetractMaxImpulseAxis,
                    armsPistonsRetractMaxImpulseNonAxis);
            }
            break;
        }
        case EArms.DownArms: 
        {
            for (int i = 0; i < this.armsDownMagneticPlates.Count; ++i) {
                this.armsDownMagneticPlates[i].AutoLock = false;
                this.armsDownMagneticPlates[i].Unlock();
            }
            
            for (int i = 0; i < this.armsDownPistons.Count; ++i) {
                this.SetPistonMovement(
                    this.armsDownPistons[i],
                    PistonMovement.Retract,
                    armsPistonsRetractSpeed,
                    armsPistonsRetractMaxImpulseAxis,
                    armsPistonsRetractMaxImpulseNonAxis);
            }
            break;
        }
        default:
            break;
    }
}

void ExtendArms(EArms arms)
{
    switch (arms)
    {
        case EArms.UpArms: 
        {
            for (int i = 0; i < this.armsUpMagneticPlates.Count; ++i) {
                this.armsUpMagneticPlates[i].AutoLock = true;
            }

            for (int i = 0; i < this.armsUpPistons.Count; ++i) {
                this.SetPistonMovement(
                    this.armsUpPistons[i],
                    PistonMovement.Extend,
                    armsPistonsExtendSpeed,
                    armsPistonsExtendMaxImpulseAxis,
                    armsPistonsExtendMaxImpulseNonAxis);
            }
            break;
        }
        case EArms.DownArms:
        {
            for (int i = 0; i < this.armsDownMagneticPlates.Count; ++i) {
                this.armsDownMagneticPlates[i].AutoLock = true;
            }

            for (int i = 0; i < this.armsDownPistons.Count; ++i) {
                this.SetPistonMovement(
                    this.armsDownPistons[i],
                    PistonMovement.Extend,
                    armsPistonsExtendSpeed,
                    armsPistonsExtendMaxImpulseAxis,
                    armsPistonsExtendMaxImpulseNonAxis);
            }
            break;
        }
        default:
            break;
    }

}

bool CheckMagneticPlates(EArms arms)
{
    switch (arms)
    {
        case EArms.UpArms: 
        {
            int locked = 0;
            for (int i = 0; i < this.armsUpMagneticPlates.Count; ++i)
            {
                if (this.armsUpMagneticPlates[i].LockMode == LandingGearMode.Locked) 
                {
                    ++locked;
                }
            }
            return locked >= minimumArmLocked;
        }
        case EArms.DownArms:
        {
            int locked = 0;
            for (int i = 0; i < this.armsDownMagneticPlates.Count; ++i)
            {
                if (this.armsDownMagneticPlates[i].LockMode == LandingGearMode.Locked) 
                {
                    ++locked;
                }
            }
            return locked >= minimumArmLocked;
        }
        default:
            return false;
    }
}

bool CheckPistonsArmsFullyRetracted(EArms arms)
{
    switch (arms)
    {
        case EArms.UpArms: 
        {
            int fullyRetracted = 0;
            for (int i = 0; i < this.armsUpPistons.Count; ++i)
            {
                if (this.armsUpPistons[i].CurrentPosition == 0.0f) 
                {
                    ++fullyRetracted;
                }
            }
            return fullyRetracted == this.armsUpPistons.Count;
        }
        case EArms.DownArms:
        {
            int fullyRetracted = 0;
            for (int i = 0; i < this.armsDownPistons.Count; ++i)
            {
                if (this.armsDownPistons[i].CurrentPosition == 0.0f) 
                {
                    ++fullyRetracted;
                }
            }
            return fullyRetracted == this.armsDownPistons.Count;
        }
        default:
            return false;
    }
}

bool CheckPistonsVPushFullyRetracted()
{
    int fullyRetracted = 0;
    for (int i = 0; i < this.verticalPushPistons.Count; ++i)
    {
        if (this.verticalPushPistons[i].CurrentPosition == 0.0f) 
        {
            ++fullyRetracted;
        }
    }
    return fullyRetracted == this.verticalPushPistons.Count;
}

bool CheckPistonsVPushForPosition(float targetPosition)
{
    int target = 0;
    for (int i = 0; i < this.verticalPushPistons.Count; ++i)
    {
        if (this.verticalPushPistons[i].CurrentPosition == targetPosition) 
        {
            ++target;
        }
    }
    return target == this.verticalPushPistons.Count;
}

void IsDrillMotorBlocked()
{
    if (Math.Abs((this.drillMotor.Angle * (180/PI)) - this.lastDrillMotorAngle) < minimumDrillRotorRotationPerExecution)
    {
        this.lastDrillMotorAngle = this.drillMotor.Angle * (180/PI);
        this.isDrillMotorBlocked = true;
    }
    else
    {
        this.lastDrillMotorAngle = this.drillMotor.Angle * (180/PI);
        this.isDrillMotorBlocked = false;
    }
}

void VPistonHeightCorrection()
{
    float referencePistonPosition = 0.0f;
    for (int i = 0; i < this.verticalPushPistons.Count; ++i)
    {
        if (i == 0)
        {
            referencePistonPosition = this.verticalPushPistons[i].CurrentPosition;
        }
        else if (this.verticalPushPistons[i].CurrentPosition - referencePistonPosition > 0.1f)
        {
            this.SetPistonMovement(
                this.verticalPushPistons[0],
                PistonMovement.Stop,
                vPistonsExpandSpeed,
                vPistonsExpandMaxImpulseAxis,
                vPistonsExpandMaxImpulseNonAxis);
            this.SetPistonMovement(
                this.verticalPushPistons[i],
                PistonMovement.Retract,
                vPistonsExpandSpeed,
                vPistonsExpandMaxImpulseAxis,
                vPistonsExpandMaxImpulseNonAxis);
        }
        else if (this.verticalPushPistons[i].CurrentPosition - referencePistonPosition < -0.1f)
        {
            this.SetPistonMovement(
                this.verticalPushPistons[0],
                PistonMovement.Stop,
                vPistonsExpandSpeed,
                vPistonsExpandMaxImpulseAxis,
                vPistonsExpandMaxImpulseNonAxis);
            this.SetPistonMovement(
                this.verticalPushPistons[i],
                PistonMovement.Extend,
                vPistonsExpandSpeed,
                vPistonsExpandMaxImpulseAxis,
                vPistonsExpandMaxImpulseNonAxis);
        }
    }
}


void DrawOnScreen(string text)
{
    if (this.lcdPrimary != null) 
    {
        this.text += ("[ DETAILS ] \n");
        this.text += '\n';
        this.text += ("Drill Motor : " + (this.isDrillMotorBlocked ? "Blocked" : "Running") + "\n");
        this.text += ("      Angle : " + (this.drillMotor.Angle * (180/PI)).ToString("N2") + "°" + "\n");
        this.text += '\n';
        this.text += ("Vert. Pist. : " + (this.currentStep == MoleSteps.RetractVPush ? "Retracting" : this.currentStep == MoleSteps.ExtendVPush ? "Extending" : "Static") + "\n");
        for (int i = 0; i < this.verticalPushPistons.Count; ++i)
        {
            this.text += (" Position " + (i + 1) + " : " + this.verticalPushPistons[i].CurrentPosition.ToString("N3") + "m" + "\n");
        }
        this.text += '\n';
        this.text += ("Up Arms     : " + (this.currentStep == MoleSteps.UnlockUpArms ? "Retracting" : this.currentStep == MoleSteps.LockUpArms ? "Extending" : "Static") + "\n");
        for (int i = 0; i < this.armsUpPistons.Count; ++i)
        {
            this.text += (" Position " + (i + 1) + " : " + this.armsUpPistons[i].CurrentPosition.ToString("N3") + "m " + (this.armsUpMagneticPlates[i] == null ? "" :
             this.armsUpMagneticPlates[i].LockMode == LandingGearMode.Locked ? "Lock" : "Unlock") + "\n");
        }
        this.text += '\n';
        this.text += ("Down Arms   : " + (this.currentStep == MoleSteps.UnlockDownArms ? "Retracting" : this.currentStep == MoleSteps.LockDownArms ? "Extending" : "Static") + "\n");
        for (int i = 0; i < this.armsDownPistons.Count; ++i)
        {
            this.text += (" Position " + (i + 1) + " : " + this.armsDownPistons[i].CurrentPosition.ToString("N3") + "m " + (this.armsDownMagneticPlates[i] == null ? "" :
             this.armsDownMagneticPlates[i].LockMode == LandingGearMode.Locked ? "Lock" : "Unlock") + "\n");
        }
        this.text += '\n';
    }

    if (this.lcdSecondary != null) 
    {
        this.textLcd2 += ("[ GENERAL ] \n");
        this.textLcd2 += '\n';
        this.textLcd2 += ("Status : " + this.currentStatus + '\n');
        this.textLcd2 += '\n';
        this.textLcd2 += ((this.currentStep == MoleSteps.Pending ? "-> " : "   ") + MoleSteps.Pending + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.Init ? "-> " : "   ") + MoleSteps.Init + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.ExtendVPush ? "  -> " : "     ") + MoleSteps.ExtendVPush + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.LockDownArms ? "  -> " : "     ") + MoleSteps.LockDownArms + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.UnlockUpArms ? "  -> " : "     ") + MoleSteps.UnlockUpArms + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.RetractVPush ? "  -> " : "     ") + MoleSteps.RetractVPush + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.LockUpArms ? "  -> " : "     ") + MoleSteps.LockUpArms + "\n");
        this.textLcd2 += ((this.currentStep == MoleSteps.UnlockDownArms ? "  -> " : "     ") + MoleSteps.UnlockDownArms + "\n");
        this.textLcd2 += "\n";
        this.textLcd2 += ("Error  : " + this.currentErrorStep + '\n');
        this.textLcd2 += new String('\n', 8);
        this.textLcd2 += "\n";
        this.textLcd2 += " RUN DOWN      RUN UP        STOP\n";
        this.textLcd2 += (currentOrder == Orders.RunDown ? " --------\n" : currentOrder == Orders.RunUp ? "               ------\n" :
         currentOrder == Orders.Stop ? "                             ----\n" : "\n");
    }

    if (this.lcdTertiary != null) 
    {
        this.inverseSketchPart = !this.inverseSketchPart;

        this.textLcd3 += ("[ SKETCH ] \n");
        this.textLcd3 += '\n';

        switch(this.currentStep)
        {
            case MoleSteps.Pending:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.Init:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.ExtendVPush:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += (this.inverseSketchPart ? "   ^ ^   \n" : "\n");
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += (this.inverseSketchPart ? "   v v   \n" : "\n");
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.LockDownArms:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += (this.inverseSketchPart ? "<--###-->\n" : " --###--\n");
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.UnlockUpArms:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += (this.inverseSketchPart ? ">--###--<\n" : " --###--\n");
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.RetractVPush:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += (this.inverseSketchPart ? "   v v   \n" : "\n");
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += (this.inverseSketchPart ? "   ^ ^   \n" : "\n");
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.LockUpArms:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += (this.inverseSketchPart ? "<--###-->\n" : " --###--\n");
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            case MoleSteps.UnlockDownArms:
            {
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "|--###--|\n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += "   | |   \n";
                this.textLcd3 += (this.inverseSketchPart ? ">--###--<\n" : " --###--\n");
                this.textLcd3 += "   ###   \n";
                this.textLcd3 += "    |    \n";
                this.textLcd3 += " ***|*** \n";
                break;
            }
            default:
                break;
        }
    }


    // int progressBarDrill = (int)(((this.drillMotor.Angle * (180.0f/PI)) / 360.0f) * 20);
    // this.text += ("Drill Motor " + "[" + new String('#', progressBarDrill) + new String('.', 20 - progressBarDrill) + "]\n");    
}

void Display()
{
    if (this.lcdPrimary != null) 
    {
        this.lcdPrimary.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
        this.lcdPrimary.WriteText(this.text, false); 
        this.text = "";
    }

    if (this.lcdSecondary != null) 
    {
        this.lcdSecondary.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
        this.lcdSecondary.WriteText(this.textLcd2, false); 
        this.textLcd2 = "";
    }

    if (this.lcdTertiary != null) 
    {
        this.lcdTertiary.Alignment = VRage.Game.GUI.TextPanel.TextAlignment.LEFT;
        this.lcdTertiary.WriteText(this.textLcd3, false); 
        this.textLcd3 = "";
    }
}

public enum EArms
{
    UpArms,
    DownArms,
}

public enum MoleSteps
{
    Pending,
    Init,
    LockDownArms,
    UnlockDownArms,
    LockUpArms,
    UnlockUpArms,
    ExtendVPush,
    RetractVPush,
}

public enum MoleErrorSteps
{
    None,
    DrillMotorBlocked,
}

public enum MoleStatus
{
    Stop,
    LoopDown,
    LoopUp,
    Error,
}

public enum Orders
{
    RunDown,
    RunUp,
    Stop,
}

public enum PistonMovement
{
    Stop,
    Extend,
    Retract,
}
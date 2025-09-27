using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;

// ------------------------------------------------------------------
// SometimesgoodSometimesbadBOT
// ------------------------------------------------------------------
// A sample bot original made for Robocode by Mathew Nelson.
//
// This robot moves in a zigzag pattern while firing at enemies.
// ------------------------------------------------------------------
public class SometimesgoodSometimesbadBOT : Bot
{
    bool movingForward;

    public double margin = 100;
    public double leftWall;
    public double rightWall;
    public double topWall;
    public double bottomWall;

    // The main method starts our bot
    static void Main()
    {
        new SometimesgoodSometimesbadBOT().Start();
    }

    // Called when a new round is started -> initialize and do some movement
    public override void Run()
    {
        BodyColor = Color.Blue;
        TurretColor = Color.Red;
        RadarColor = Color.Black;
        BulletColor = Color.OrangeRed;
        ScanColor = Color.Yellow;

        CalculateWalls();

        movingForward = true;

        // Loop while as long as the bot is running
        while (IsRunning)
        {
            GunTurnRate = 20;
            MaxSpeed = 6;
            // Tell the game we will want to move ahead 40000 -- some large number
            SetForward(40000);
            movingForward = true;
            // Tell the game we will want to turn right 90
            SetTurnLeft(90);
            // At this point, we have indicated to the game that *when we do something*,
            // we will want to move ahead and turn right. That's what "Set" means.
            // It is important to realize we have not done anything yet!
            // In order to actually move, we'll want to call a method that takes real time, such as
            // WaitFor.
            // WaitFor actually starts the action -- we start moving and turning.
            // It will not return until we have finished turning.
            WaitFor(new TurnCompleteCondition(this));
            // Note: We are still moving ahead now, but the turn is complete.
            // Now we'll turn the other way...
            SetTurnRight(180);
            // ... and wait for the turn to finish ...
            WaitFor(new TurnCompleteCondition(this));
            // ... then the other way ...
            SetTurnLeft(180);
            // ... and wait for that turn to finish.
            WaitFor(new TurnCompleteCondition(this));
            // then back to the top to do it all again.

            if (X < leftWall || X > rightWall || Y < topWall || Y > bottomWall)
            {
                //nothing
            }
            else
            {
                ReverseDirection();
            }
        }
    }

    // We collided with a wall -> reverse the direction
    public override void OnHitWall(HitWallEvent e)
    {
        // Bounce off!
        ReverseDirection();
    }

    // ReverseDirection: Switch from ahead to back & vice versa
    public void ReverseDirection()
    {
        if (movingForward)
        {
            SetBack(40000);
            movingForward = false;
        }
        else
        {
            SetForward(40000);
            movingForward = true;
        }
    }

    // We scanned another bot -> fire!
    public override void OnScannedBot(ScannedBotEvent e)
    {
        var distance = DistanceTo(e.X, e.Y);
        if (distance > 500)
        {
            Fire(1);
        }
        else if (distance > 250)
        {
            Fire(2);
        }
        else if (distance > 125)
        {
            Fire(3);
            ReverseDirection();
        }
        else
        {
            Fire(3);
        }

    }

    // We hit another bot -> back up!
    public override void OnHitBot(HitBotEvent e)
    {
        var bearing = BearingTo(e.X, e.Y);
        if (bearing > -10 && bearing < 10)
        {
            Fire(3);
            
        }
        if (e.IsRammed)
        {
            TurnRight(10);
        }
        
        // If we're moving into the other bot, reverse!
        if (e.IsRammed)
        {
            ReverseDirection();
        }
    }

    public void CalculateWalls()
    {

        leftWall = margin;
        rightWall = ArenaWidth - margin;
        topWall = margin;
        bottomWall = ArenaHeight - margin;
    }
    
    
}

// Condition that is triggered when the turning is complete
public class TurnCompleteCondition : Condition
{
    private readonly Bot bot;

    public TurnCompleteCondition(Bot bot)
    {
        this.bot = bot;
    }

    public override bool Test()
    {
        // turn is complete when the remainder of the turn is zero
        return bot.TurnRemaining == 0;
    }
}
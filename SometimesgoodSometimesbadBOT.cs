using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.BotApi.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

// ------------------------------------------------------------------
// SometimesgoodSometimesbadBOT - ADVANCED UNSTOPPABLE VERSION
// ------------------------------------------------------------------
// Enhanced with predictive targeting, anti-gravity movement,
// lock-on missiles, radar management, and strategic AI
// ------------------------------------------------------------------
public class SometimesgoodSometimesbadBOT : Bot
{
    // Movement variables
    bool movingForward;
    private int moveDirection = 1;
    private double lastEnemyBearing;
    
    // Targeting system variables
    private Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
    private string lockOnTarget = null;
    private double lockOnPower = 3.0;
    private int lockOnDuration = 0;
    
    // Radar management
    private string lastScannedEnemy = null;
    private double radarDirection = 1;
    private bool wideSearch = true;
    
    // Strategic AI
    private int enemyCount = 0;
    private GamePhase currentPhase = GamePhase.Early;
    private double conserveEnergyThreshold = 20.0;
    
    // Performance tracking
    private int shotsFired = 0;
    private int shotsHit = 0;
    private long lastDamageTime = 0;

    // The main method starts our bot
    static void Main()
    {
        new SometimesgoodSometimesbadBOT().Start();
    }

    // Called when a new round is started -> initialize and do advanced behavior
    public override void Run()
    {
        // Enhanced visual configuration
        BodyColor = Color.DarkBlue;
        TurretColor = Color.Crimson;
        RadarColor = Color.Gold;
        BulletColor = Color.OrangeRed;
        ScanColor = Color.Cyan;

        // Initialize
        movingForward = true;
        SetAdjustGunForBodyTurn(true);
        SetAdjustRadarForGunTurn(true);

        // Main combat loop
        while (IsRunning)
        {
            UpdateGamePhase();
            PerformRadarSweep();
            ExecuteAdvancedMovement();
            ManageTargeting();
            Execute();
        }
    }

    // Advanced radar management with intelligent sweeping
    private void PerformRadarSweep()
    {
        if (lockOnTarget != null && enemies.ContainsKey(lockOnTarget))
        {
            // Lock-on radar tracking
            var enemy = enemies[lockOnTarget];
            var absoluteBearing = Bearing + enemy.Bearing;
            var radarTurn = NormalizeBearing(absoluteBearing - RadarBearing);
            
            if (Math.Abs(radarTurn) < 6)
            {
                radarTurn = 6 * Math.Sign(radarTurn);
            }
            
            SetTurnRadarRight(radarTurn);
        }
        else
        {
            // Wide search pattern
            if (lastScannedEnemy == null || Time % 20 == 0)
            {
                SetTurnRadarRight(360 * radarDirection);
                if (Time % 100 == 0) radarDirection *= -1;
            }
            else
            {
                // Narrow sweep around last known position
                SetTurnRadarRight(30 * radarDirection);
            }
        }
    }

    // Anti-gravity movement with wall smoothing
    private void ExecuteAdvancedMovement()
    {
        if (enemies.Count == 0)
        {
            // Basic patrol when no enemies
            SetTurnRight(5);
            SetForward(100);
            return;
        }

        var forceX = 0.0;
        var forceY = 0.0;

        // Anti-gravity forces from enemies
        foreach (var enemy in enemies.Values)
        {
            var angle = Math.ToRadians(Bearing + enemy.Bearing);
            var distance = Math.Max(enemy.Distance, 50);
            
            // Calculate repulsion force (stronger for closer/more dangerous enemies)
            var force = (enemy.ThreatLevel * 10000) / (distance * distance);
            
            forceX -= Math.Sin(angle) * force;
            forceY -= Math.Cos(angle) * force;
        }

        // Wall smoothing - avoid getting stuck against walls
        var wallMargin = 80.0;
        
        if (X < wallMargin) forceX += 5000 / (X + 1);
        if (Y < wallMargin) forceY += 5000 / (Y + 1);
        if (X > BattleFieldWidth - wallMargin) forceX -= 5000 / (BattleFieldWidth - X + 1);
        if (Y > BattleFieldHeight - wallMargin) forceY -= 5000 / (BattleFieldHeight - Y + 1);

        // Calculate movement direction
        var angle = Math.Atan2(forceX, forceY);
        var desiredHeading = Math.ToDegrees(angle);
        
        var turn = NormalizeBearing(desiredHeading - Bearing);
        
        // Optimized turning and movement
        if (Math.Abs(turn) > 90)
        {
            turn = NormalizeBearing(turn + 180);
            SetBack(100);
        }
        else
        {
            SetForward(100);
        }
        
        SetTurnRight(turn);
    }

    // Strategic AI and game phase management
    private void UpdateGamePhase()
    {
        enemyCount = enemies.Count;
        
        if (enemyCount > 5) currentPhase = GamePhase.Early;
        else if (enemyCount > 2) currentPhase = GamePhase.Mid;
        else currentPhase = GamePhase.Late;
        
        // Adjust strategy based on phase
        switch (currentPhase)
        {
            case GamePhase.Early:
                conserveEnergyThreshold = 15.0;
                wideSearch = true;
                break;
            case GamePhase.Mid:
                conserveEnergyThreshold = 25.0;
                wideSearch = false;
                break;
            case GamePhase.Late:
                conserveEnergyThreshold = 35.0;
                wideSearch = false;
                SelectPriorityTarget();
                break;
        }
    }

    // Advanced targeting management
    private void ManageTargeting()
    {
        if (lockOnTarget != null)
        {
            lockOnDuration--;
            if (lockOnDuration <= 0 || !enemies.ContainsKey(lockOnTarget))
            {
                lockOnTarget = null;
            }
        }
        
        // Clean up old enemy data
        var currentTime = Time;
        var enemiesToRemove = enemies.Where(e => currentTime - e.Value.Time > 30).Select(e => e.Key).ToList();
        foreach (var enemyName in enemiesToRemove)
        {
            enemies.Remove(enemyName);
        }
    }

    // Select highest priority target for lock-on
    private void SelectPriorityTarget()
    {
        if (enemies.Count == 0) return;
        
        var priorityTarget = enemies.Values
            .OrderByDescending(e => e.ThreatLevel)
            .ThenBy(e => e.Distance)
            .FirstOrDefault();
            
        if (priorityTarget != null)
        {
            lockOnTarget = priorityTarget.Name;
            lockOnDuration = 50; // Lock for 50 ticks
        }
    }

    // Utility method to normalize bearing angles
    private double NormalizeBearing(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    // Advanced wall collision handling with evasive maneuvers
    public override void OnHitWall(HitWallEvent e)
    {
        // Execute evasive wall escape maneuver
        PerformWallEscape();
    }

    // Intelligent wall escape with randomization
    private void PerformWallEscape()
    {
        var random = new Random();
        var escapeAngle = random.Next(60, 120); // Random angle between 60-120 degrees
        
        if (random.NextDouble() > 0.5)
        {
            escapeAngle = -escapeAngle;
        }
        
        SetTurnRight(escapeAngle);
        SetForward(100);
        
        // Add some unpredictability
        moveDirection *= -1;
    }

    // Enhanced direction reversal with tactical considerations
    public void ReverseDirection()
    {
        var newDistance = Math.Min(150, Energy * 2); // Scale with energy
        
        if (movingForward)
        {
            SetBack(newDistance);
            movingForward = false;
        }
        else
        {
            SetForward(newDistance);
            movingForward = true;
        }
    }

    // Advanced targeting with predictive firing and lock-on missiles
    public override void OnScannedBot(ScannedBotEvent e)
    {
        lastScannedEnemy = e.Name;
        
        // Update or create enemy data
        if (!enemies.ContainsKey(e.Name))
        {
            enemies[e.Name] = new EnemyData { Name = e.Name };
        }
        
        var enemy = enemies[e.Name];
        var distance = DistanceTo(e.X, e.Y);
        
        // Update enemy information
        enemy.X = e.X;
        enemy.Y = e.Y;
        enemy.Bearing = e.Bearing;
        enemy.Distance = distance;
        enemy.Energy = e.Energy;
        enemy.Heading = e.Heading;
        enemy.Velocity = e.Velocity;
        enemy.Time = Time;
        enemy.UpdateHistory(e.X, e.Y);
        
        // Calculate threat level
        enemy.ThreatLevel = CalculateThreatLevel(enemy);
        
        // Predictive targeting
        var prediction = PredictEnemyPosition(enemy);
        if (prediction != null)
        {
            var targetBearing = BearingTo(prediction.X, prediction.Y);
            var gunTurn = NormalizeBearing(targetBearing - GunBearing);
            SetTurnGunRight(gunTurn);
            
            // Lock-on missile system
            if (lockOnTarget == e.Name || ShouldEngageLockOn(enemy))
            {
                FireLockOnMissile(enemy, prediction);
            }
            else
            {
                FireOptimalBullet(enemy, distance);
            }
        }
        else
        {
            // Fallback to direct targeting
            var gunTurn = NormalizeBearing(e.Bearing - (GunBearing - Bearing));
            SetTurnGunRight(gunTurn);
            FireOptimalBullet(enemy, distance);
        }
    }

    // Calculate enemy threat level for prioritization
    private double CalculateThreatLevel(EnemyData enemy)
    {
        var threatLevel = 1.0;
        
        // Distance factor (closer = more dangerous)
        threatLevel += Math.Max(0, (400 - enemy.Distance) / 400 * 2);
        
        // Energy factor (high energy = more dangerous)
        threatLevel += enemy.Energy / 100;
        
        // Velocity factor (moving targets are more dangerous)
        threatLevel += Math.Abs(enemy.Velocity) / 8;
        
        // Recent damage factor
        if (Time - lastDamageTime < 10)
        {
            threatLevel += 1.5;
        }
        
        return threatLevel;
    }

    // Predictive targeting algorithm
    private Point PredictEnemyPosition(EnemyData enemy)
    {
        if (enemy.History.Count < 2) return null;
        
        var distance = enemy.Distance;
        var bulletSpeed = 20 - 3 * GetOptimalBulletPower(distance);
        var timeToTarget = distance / bulletSpeed;
        
        // Linear prediction
        var deltaX = 0.0;
        var deltaY = 0.0;
        
        if (enemy.History.Count >= 2)
        {
            var last = enemy.History[enemy.History.Count - 1];
            var prev = enemy.History[enemy.History.Count - 2];
            
            deltaX = last.X - prev.X;
            deltaY = last.Y - prev.Y;
        }
        
        var predictedX = enemy.X + deltaX * timeToTarget;
        var predictedY = enemy.Y + deltaY * timeToTarget;
        
        // Keep prediction within battlefield bounds
        predictedX = Math.Max(0, Math.Min(BattleFieldWidth, predictedX));
        predictedY = Math.Max(0, Math.Min(BattleFieldHeight, predictedY));
        
        return new Point { X = predictedX, Y = predictedY };
    }

    // Lock-on missile system
    private bool ShouldEngageLockOn(EnemyData enemy)
    {
        // Engage lock-on for high-priority targets
        return enemy.ThreatLevel > 3.0 || 
               enemy.Distance < 200 || 
               currentPhase == GamePhase.Late ||
               enemy.Energy < 20;
    }

    private void FireLockOnMissile(EnemyData enemy, Point prediction)
    {
        if (Energy < conserveEnergyThreshold) return;
        
        lockOnTarget = enemy.Name;
        lockOnDuration = 30;
        
        // High-power missile for lock-on targets
        var bulletPower = Math.Min(lockOnPower, Energy);
        bulletPower = Math.Min(bulletPower, enemy.Energy / 4); // Don't overkill
        
        if (GunHeat == 0 && bulletPower >= 0.1)
        {
            Fire(bulletPower);
            shotsFired++;
        }
    }

    // Optimized bullet power calculation
    private void FireOptimalBullet(EnemyData enemy, double distance)
    {
        if (Energy < 5) return; // Conserve energy when low
        
        var bulletPower = GetOptimalBulletPower(distance);
        
        // Energy management
        if (Energy < conserveEnergyThreshold)
        {
            bulletPower = Math.Min(bulletPower, 1.5);
        }
        
        // Don't overkill low-energy enemies
        if (enemy.Energy < bulletPower * 4)
        {
            bulletPower = Math.Max(0.1, enemy.Energy / 4);
        }
        
        if (GunHeat == 0 && bulletPower >= 0.1)
        {
            Fire(bulletPower);
            shotsFired++;
        }
    }

    // Calculate optimal bullet power based on distance and situation
    private double GetOptimalBulletPower(double distance)
    {
        if (distance > 400) return 1.0;
        if (distance > 200) return 2.0;
        if (distance > 100) return 2.5;
        if (distance > 50) return 3.0;
        return Math.Min(3.0, Energy / 5); // Conservative at close range
    }

    // Enhanced bot collision handling with ramming strategy
    public override void OnHitBot(HitBotEvent e)
    {
        // Aggressive ramming strategy for low-energy enemies
        if (e.Energy < Energy / 2 && Energy > 20)
        {
            // Continue ramming weak opponents
            Fire(Math.Min(3.0, Energy / 5));
            SetForward(50);
        }
        else
        {
            // Evasive maneuver for stronger opponents
            Fire(2);
            PerformEvasiveManeuver();
        }
    }

    // Bullet hit confirmation - update accuracy tracking
    public override void OnBulletHit(BulletHitEvent e)
    {
        shotsHit++;
        
        // Boost lock-on duration for successful hits
        if (lockOnTarget == e.Name)
        {
            lockOnDuration += 10;
        }
    }

    // Bullet missed - adjust targeting
    public override void OnBulletMissed(BulletMissedEvent e)
    {
        // Reduce confidence in current lock-on target
        if (lockOnTarget != null)
        {
            lockOnDuration = Math.Max(0, lockOnDuration - 5);
        }
    }

    // We've been hit - implement damage response
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        lastDamageTime = Time;
        
        // Increase threat level of attacker
        if (enemies.ContainsKey(e.Name))
        {
            enemies[e.Name].ThreatLevel += 0.5;
        }
        
        // Emergency evasive action
        PerformEvasiveManeuver();
        
        // Counter-attack if we have a lock
        if (lockOnTarget == e.Name && GunHeat == 0)
        {
            Fire(Math.Min(3.0, Energy / 4));
        }
    }

    // Death event - analyze performance
    public override void OnDeath(DeathEvent e)
    {
        // Calculate final accuracy for learning
        var accuracy = shotsFired > 0 ? (double)shotsHit / shotsFired : 0;
        // This could be logged or used for adaptive learning in future rounds
    }

    // Perform evasive maneuver when under threat
    private void PerformEvasiveManeuver()
    {
        var random = new Random();
        var evasiveAngle = random.Next(45, 135);
        
        if (random.NextDouble() > 0.5)
        {
            evasiveAngle = -evasiveAngle;
        }
        
        SetTurnRight(evasiveAngle);
        
        // Quick burst movement
        if (movingForward)
        {
            SetBack(80);
        }
        else
        {
            SetForward(80);
        }
        
        ReverseDirection();
    }
}

// Supporting classes and enums for advanced bot functionality
public enum GamePhase { Early, Mid, Late }

public class EnemyData
{
    public string Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Bearing { get; set; }
    public double Distance { get; set; }
    public double Energy { get; set; }
    public double Heading { get; set; }
    public double Velocity { get; set; }
    public long Time { get; set; }
    public List<Point> History { get; set; } = new List<Point>();
    public double ThreatLevel { get; set; }
    
    public void UpdateHistory(double x, double y)
    {
        History.Add(new Point { X = x, Y = y });
        if (History.Count > 10) History.RemoveAt(0); // Keep last 10 positions
    }
}

public class Point
{
    public double X { get; set; }
    public double Y { get; set; }
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
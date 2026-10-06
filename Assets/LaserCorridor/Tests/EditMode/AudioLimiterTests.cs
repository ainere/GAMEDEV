using NUnit.Framework;
namespace LaserCorridor.Tests
{
    public class AudioLimiterTests
    {
        [Test] public void FeedbackVoicesNeverOverlapAndIntervalIsEnforced()
        {
            var limiter=new AudioCueLimiter();Assert.That(limiter.TryStart("pickup",0,.5f,.15f,1),Is.True);Assert.That(limiter.TryStart("pickup",.2f,.5f,.15f,1),Is.False);Assert.That(limiter.Active("pickup",.4f),Is.EqualTo(1));Assert.That(limiter.TryStart("pickup",.5f,.5f,.15f,1),Is.True);
            limiter.Clear();Assert.That(limiter.TryStart("impact",0,.05f,.2f,1),Is.True);Assert.That(limiter.TryStart("impact",.1f,.05f,.2f,1),Is.False);Assert.That(limiter.TryStart("impact",.2f,.05f,.2f,1),Is.True);
        }
        [Test] public void PauseTimeAndStopPreserveVoiceBounds()
        {var limiter=new AudioCueLimiter();limiter.TryStart("pickup",2,.5f,0,1);Assert.That(limiter.Active("pickup",2),Is.EqualTo(1));Assert.That(limiter.TryStart("pickup",2,.5f,0,1),Is.False);limiter.Clear();Assert.That(limiter.Active("pickup",2),Is.Zero);Assert.That(limiter.TryStart("pickup",2,.5f,0,1),Is.True);}
    }
}

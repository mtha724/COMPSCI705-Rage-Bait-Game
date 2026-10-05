using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class PlatformerValidation
{
    static IEnumerator OverboardFeedbackPlayChecks()
    {
        yield return new Await(() => GameManager.Instance != null);
        var manager = GameManager.Instance;
        var fx = manager.GetComponent<FXController>();
        var cueSource = manager.GetComponents<AudioSource>().Last();
        int starts = 0, revivals = 0;
        manager.RunStarted += () => starts++;
        manager.Revived += () => revivals++;
        manager.condition = FXCondition.Overboard;
        manager.participantId = "automated-feedback-validation";
        var profile = fx.Profile;
        Assert(profile.deathClipOverride != null && profile.reviveClip != null &&
            profile.runStartClip != null && profile.fallingBlockImpactClip != null,
            "All four selected Overboard clips resolve through their saved asset references");
        foreach (var clip in new[] { profile.deathClipOverride, profile.reviveClip, profile.runStartClip, profile.fallingBlockImpactClip })
            clip.LoadAudioData();
        yield return new Await(() => profile.runStartClip.loadState == AudioDataLoadState.Loaded);
        Assert(Mathf.Abs(profile.reviveClip.length - 2f) < .05f && Mathf.Abs(profile.runStartClip.length - 3f) < .05f &&
            Mathf.Abs(profile.fallingBlockImpactClip.length - 1f) < .05f, "Collection clips use the selected timestamp ranges");
        Assert(!cueSource.isPlaying, "Opening sound waits for gameplay rather than playing in the menu");

        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing && starts == 1);
        yield return .1f;
        Assert(cueSource.isPlaying && revivals == 0, "Starting a run plays the cave cue once without a revival cue");
        yield return 3.1f;

        // Use isolated physical objects so this check remains valid when authored Level 3 blocks move.
        var support = new GameObject("FeedbackValidationFloor");
        support.layer = LayerMask.NameToLayer("Ground");
        support.transform.position = new Vector3(-20f, -3f);
        support.AddComponent<BoxCollider2D>().size = new Vector2(8f, .5f);
        var block = new GameObject("FeedbackValidationBlock");
        block.transform.position = new Vector3(-20f, 0f);
        block.AddComponent<BoxCollider2D>().size = Vector2.one;
        var falling = block.AddComponent<FallingObject>();
        var body = block.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 10f;
        falling.delay = 0f;
        falling.Activate();
        yield return new Await(() => cueSource.isPlaying);
        Assert(block.transform.position.y < -1.5f && !falling.disappearOnLanding,
            "A retained falling block plays the impact cue on its real ground landing");
        yield return 1.15f;
        Assert(!cueSource.isPlaying, "Impact cue finishes after the selected one-second segment");
        body.position = new Vector2(-20f, 0f);
        body.linearVelocity = Vector2.down;
        yield return .7f;
        Assert(!cueSource.isPlaying, "A repeated landing does not replay the block's first-impact cue");
        UnityEngine.Object.Destroy(block);
        UnityEngine.Object.Destroy(support);

        var oldPlayer = manager.Player;
        manager.Die("feedback_validation");
        Assert(fx.Message == "YOU DIED", "Overboard death message is all caps");
        Assert(oldPlayer.GetComponentsInChildren<SpriteRenderer>().All(s => s.color == Color.red),
            "Overboard death turns the player's sprites red");
        Assert(cueSource.isPlaying, "Overboard death starts the selected death cue");
        yield return .1f;
        var text = GameObject.Find("DeathMessage").GetComponent<Text>();
        Assert(text.text == "YOU DIED" && text.fontStyle == FontStyle.Bold && text.fontSize == 112,
            "Death HUD renders the configured large bold all-caps text");
        Assert(text.GetComponents<Outline>().Length == 2 && text.GetComponents<Outline>().All(o => o.enabled),
            "Overboard glyphs are thickened and have a dark outer outline");
        var backdrop = GameObject.Find("DeathMessageBackdrop").GetComponent<Image>();
        Assert(backdrop.gameObject.activeSelf && Contrast(text.color, backdrop.color) >= 4.5f,
            "Red death text has at least 4.5:1 contrast against its dark backing");
        yield return new Await(() => manager.State == RunState.Playing && revivals == 1);
        Assert(manager.Player != oldPlayer && manager.CurrentLevel == "Level 1" && manager.Deaths == 1,
            "Death still reloads Level 1 and retains the death count");
        Assert(manager.Player.GetComponentsInChildren<SpriteRenderer>().All(s => s.color != Color.red),
            "Revival restores the fresh player's normal sprite colour");
        Assert(cueSource.isPlaying, "Revival plays Eye of Rah on the persistent cue source");
        yield return .1f;
        Assert(string.IsNullOrEmpty(fx.Message) && !backdrop.gameObject.activeSelf && fx.ScreenColour.a == 0f,
            "Revival clears the death message, backing and screen overlay");

        manager.ReachGoal("Level 2");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 2");
        Assert(starts == 1 && revivals == 1, "Advancing a level plays neither a new-run nor a revival event");
        yield return 3.1f;

        manager.condition = FXCondition.Normal;
        manager.Die("normal_feedback_validation");
        Assert(fx.Message == "You died" && manager.Player.GetComponentsInChildren<SpriteRenderer>().All(s => s.color != Color.red),
            "Normal deaths retain their original message and player colour");
        yield return .1f;
        Assert(text.fontStyle == FontStyle.Normal && text.color == Color.white && text.GetComponents<Outline>().All(o => !o.enabled),
            "Normal death text retains its original white regular styling");
        Assert(!cueSource.isPlaying && !backdrop.gameObject.activeSelf, "Normal death adds no meme cue or Overboard backing");
        yield return new Await(() => manager.State == RunState.Playing && revivals == 2);
        Assert(!cueSource.isPlaying, "Normal revival adds no Overboard cue");

        manager.StopRun("feedback_validation");
        manager.condition = FXCondition.Low;
        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing && starts == 2);
        Assert(!cueSource.isPlaying, "Low condition adds no opening cue");
        fx.FallingBlockImpact();
        Assert(!cueSource.isPlaying, "Low condition adds no falling-block cue");
        manager.StopRun("feedback_validation_complete");
    }

    static float Contrast(Color first, Color second)
    {
        float a = Luminance(first), b = Luminance(second);
        return (Mathf.Max(a, b) + .05f) / (Mathf.Min(a, b) + .05f);
    }

    static float Luminance(Color colour) => .2126f * Linear(colour.r) + .7152f * Linear(colour.g) + .0722f * Linear(colour.b);
    static float Linear(float channel) => channel <= .04045f ? channel / 12.92f : Mathf.Pow((channel + .055f) / 1.055f, 2.4f);
}

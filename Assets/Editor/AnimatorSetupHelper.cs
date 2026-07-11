#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorSetupHelper
{
    public static void Execute()
    {
        Debug.Log("[AnimatorSetupHelper] Starting animator setup...");

        string savePath = "Assets/3DNPC_Characters/Female_NPC/PlayerAnimator.controller";
        
        // 1. Create AnimatorController
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);

        // 2. Add Parameters
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("LastMoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("LastMoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        // Get Root State Machine
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // 3. Create Idle Blend Tree State
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        
        BlendTree idleTree = new BlendTree();
        idleTree.name = "IdleTree";
        idleTree.blendType = BlendTreeType.SimpleDirectional2D;
        idleTree.blendParameter = "LastMoveX";
        idleTree.blendParameterY = "LastMoveY";
        
        AssetDatabase.AddObjectToAsset(idleTree, controller);
        idleState.motion = idleTree;

        // Load Idle Clips
        AnimationClip f1DownIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_down_idle.anim");
        AnimationClip f1UpIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_up_idle.anim");
        AnimationClip f1SideIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_side_idle.anim");

        if (f1DownIdle != null) idleTree.AddChild(f1DownIdle, new Vector2(0f, -1f));
        if (f1UpIdle != null) idleTree.AddChild(f1UpIdle, new Vector2(0f, 1f));
        if (f1SideIdle != null)
        {
            idleTree.AddChild(f1SideIdle, new Vector2(-1f, 0f));
            idleTree.AddChild(f1SideIdle, new Vector2(1f, 0f));
        }

        // 4. Create Walk Blend Tree State
        AnimatorState walkState = rootStateMachine.AddState("Walk");
        
        BlendTree walkTree = new BlendTree();
        walkTree.name = "WalkTree";
        walkTree.blendType = BlendTreeType.SimpleDirectional2D;
        walkTree.blendParameter = "MoveX";
        walkTree.blendParameterY = "MoveY";
        
        AssetDatabase.AddObjectToAsset(walkTree, controller);
        walkState.motion = walkTree;

        // Load Walk Clips
        AnimationClip f1DownWalk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_down_walk.anim");
        AnimationClip f1UpWalk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_up_walk.anim");
        AnimationClip f1SideWalk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3DNPC_Characters/Female_NPC/F1_side_walk.anim");

        if (f1DownWalk != null) walkTree.AddChild(f1DownWalk, new Vector2(0f, -1f));
        if (f1UpWalk != null) walkTree.AddChild(f1UpWalk, new Vector2(0f, 1f));
        if (f1SideWalk != null)
        {
            walkTree.AddChild(f1SideWalk, new Vector2(-1f, 0f));
            walkTree.AddChild(f1SideWalk, new Vector2(1f, 0f));
        }

        // 5. Setup Transitions
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        // Set Default State
        rootStateMachine.defaultState = idleState;

        // 6. Update Player Object's Animator Controller in Persistent Scene
        string persistentPath = "Assets/Scenes/Persistent.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(persistentPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Animator anim = playerObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
                Debug.Log("[AnimatorSetupHelper] Successfully updated Player Animator to: " + savePath);
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("[AnimatorSetupHelper] Animator setup complete!");
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为尚未在场景中手工配置的 Objective 建立公共 Hint 层级。
///
/// 生成格式：
/// HintTargets
/// └── XxxHintTarget
///     ├── ObjectiveHintTarget
///     └── Outline_TargetName
///         └── ObjectiveHintOutline
///
/// Target/Outline 本身始终 Active；实际 Renderer 可以位于会切换
/// Active 状态的 Player/UFO/Level2 层级中。
/// </summary>
public static class ObjectiveHintRuntimeTargetBuilder
{
    private const string HintRootName = "HintTargets";

    public static ObjectiveHintTarget ResolveOrCreate(
        ObjectiveHintTarget configuredTarget,
        string targetName,
        GameObject rendererRoot)
    {
        ObjectiveHintOutline[] ignored;

        return ResolveOrCreate(
            configuredTarget,
            targetName,
            rendererRoot != null
                ? new[] { rendererRoot }
                : new GameObject[0],
            out ignored
        );
    }

    public static ObjectiveHintTarget ResolveOrCreate(
        ObjectiveHintTarget configuredTarget,
        string targetName,
        GameObject[] rendererRoots,
        out ObjectiveHintOutline[] outlines)
    {
        if (configuredTarget != null)
        {
            outlines = configuredTarget
                .GetOutlineTargetsSnapshot();

            return configuredTarget;
        }

        Transform hintRoot = ResolveHintRoot();
        Transform existing = hintRoot.Find(targetName);

        GameObject targetObject;

        if (existing != null)
        {
            targetObject = existing.gameObject;
        }
        else
        {
            targetObject = new GameObject(targetName);
            targetObject.transform.SetParent(
                hintRoot,
                false
            );
        }

        ObjectiveHintTarget target =
            targetObject.GetComponent
                <ObjectiveHintTarget>();

        if (target == null)
        {
            target = targetObject.AddComponent
                <ObjectiveHintTarget>();
        }

        outlines = BuildOutlines(
            targetObject.transform,
            rendererRoots
        );

        target.Configure(
            null,
            outlines,
            false
        );

        return target;
    }

    private static Transform ResolveHintRoot()
    {
        GameObject root = GameObject.Find(
            HintRootName
        );

        if (root == null)
        {
            root = new GameObject(HintRootName);
        }

        return root.transform;
    }

    private static ObjectiveHintOutline[]
        BuildOutlines(
            Transform targetRoot,
            GameObject[] rendererRoots)
    {
        List<ObjectiveHintOutline> outlines =
            new List<ObjectiveHintOutline>();

        if (rendererRoots == null)
            return outlines.ToArray();

        for (int i = 0;
             i < rendererRoots.Length;
             i++)
        {
            GameObject rendererRoot =
                rendererRoots[i];

            if (rendererRoot == null)
                continue;

            string outlineName =
                "Outline_" +
                rendererRoot.name +
                "_" + i;

            Transform existing =
                targetRoot.Find(outlineName);

            GameObject outlineObject;

            if (existing != null)
            {
                outlineObject = existing.gameObject;
            }
            else
            {
                outlineObject =
                    new GameObject(outlineName);

                outlineObject.transform.SetParent(
                    targetRoot,
                    false
                );
            }

            ObjectiveHintOutline outline =
                outlineObject.GetComponent
                    <ObjectiveHintOutline>();

            if (outline == null)
            {
                outline = outlineObject.AddComponent
                    <ObjectiveHintOutline>();
            }

            outline.ConfigureRenderers(
                CollectOutlineRenderers(
                    rendererRoot
                )
            );

            outline.SetHintEligible(true);
            outlines.Add(outline);
        }

        return outlines.ToArray();
    }

    private static Renderer[] CollectOutlineRenderers(
        GameObject rendererRoot)
    {
        Renderer[] allRenderers =
            rendererRoot.GetComponentsInChildren
                <Renderer>(true);

        List<Renderer> usableRenderers =
            new List<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer is MeshRenderer ||
                renderer is SkinnedMeshRenderer)
            {
                usableRenderers.Add(renderer);
            }
        }

        return usableRenderers.ToArray();
    }
}

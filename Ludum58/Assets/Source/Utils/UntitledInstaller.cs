using UnityEngine;
using Zenject;

public class UntitledInstaller : MonoInstaller
{
    [SerializeField] private AudioData audioData;
    [SerializeField] private FloatData floatData;
    [SerializeField] private IntData intData;
    [SerializeField] private LayerData layerData;
    [SerializeField] private TextData textData;
    [SerializeField] private ParticleData particleData;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private Star star;
    [SerializeField] private Meteor meteor;
    [SerializeField] private TrashDamage trashDamage;

    [SerializeField] private HealOrb healOrb;
    [SerializeField] private ShieldOrb shieldOrb; // префаб щита
    [SerializeField] private SlowOrb slowOrb;     // новый префаб замедления

    [SerializeField] private SingleTunnelSpawner singleTunnelSpawner;
    [SerializeField] private CutsceneOnClick cutsceneOnClick;
    [SerializeField] private Score score;

    public override void InstallBindings()
    {
        // Сервисы и данные
        Container.Bind<ISound>().To<Sound>().AsSingle().NonLazy();
        Container.Bind<Score>().FromInstance(score).AsSingle();

        Container.Bind<PlayerController>().FromInstance(playerController).AsSingle();
        Container.Bind<PlayerStats>().FromInstance(playerStats).AsSingle().NonLazy();

        Container.Bind<SingleTunnelSpawner>().FromInstance(singleTunnelSpawner).AsSingle();
        Container.Bind<ParticleData>().FromInstance(particleData).AsSingle().NonLazy();
        Container.Bind<AudioData>().FromInstance(audioData).AsSingle().NonLazy();
        Container.Bind<FloatData>().FromInstance(floatData).AsSingle().NonLazy();
        Container.Bind<IntData>().FromInstance(intData).AsSingle().NonLazy();
        Container.Bind<LayerData>().FromInstance(layerData).AsSingle().NonLazy();
        Container.Bind<TextData>().FromInstance(textData).AsSingle().NonLazy();

        Container.Bind<CutsceneOnClick>().FromInstance(cutsceneOnClick).AsSingle().NonLazy();

        // Компоненты
        Container.Bind<Star>().FromInstance(star).AsTransient();
        Container.Bind<Meteor>().FromInstance(meteor).AsTransient();
        Container.Bind<TrashDamage>().FromInstance(trashDamage).AsTransient();
        Container.Bind<HealOrb>().FromInstance(healOrb).AsTransient();
        Container.Bind<ShieldOrb>().FromInstance(shieldOrb).AsTransient();
        Container.Bind<SlowOrb>().FromInstance(slowOrb).AsTransient();

        // Фабрики
        Container.BindFactory<LayerData, PlayerStats, Meteor, Meteor.MeteorFactory>()
                 .FromComponentInNewPrefab(meteor);

        Container.BindFactory<LayerData, PlayerController, Star, Star.StarFactory>()
                 .FromComponentInNewPrefab(star);

        Container.BindFactory<LayerData, PlayerStats, TrashDamage, TrashDamage.TrashFactory>()
                 .FromComponentInNewPrefab(trashDamage);

        Container.BindFactory<HealOrb, HealOrb.HealOrbFactory>()
                 .FromComponentInNewPrefab(healOrb)
                 .UnderTransformGroup("HealOrbs");

        Container.BindFactory<ShieldOrb, ShieldOrb.ShieldOrbFactory>()
                 .FromComponentInNewPrefab(shieldOrb)
                 .UnderTransformGroup("ShieldOrbs");

        // Новая фабрика SlowOrb
        Container.BindFactory<SlowOrb, SlowOrb.SlowOrbFactory>()
                 .FromComponentInNewPrefab(slowOrb)
                 .UnderTransformGroup("SlowOrbs");
    }
}
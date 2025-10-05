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
    [SerializeField] private Star star;
    [SerializeField] private Meteor meteor;
   // [SerializeField] private Score score;
    public override void InstallBindings()
    {
        //Container.Bind<ISound>().To<Sound>().AsSingle().NonLazy();
      //  Container.Bind<Score>().FromInstance(score).AsSingle();
        Container.Bind<PlayerController>().FromInstance(playerController).AsSingle();
        Container.Bind<Star>().FromInstance(star).AsTransient();
        Container.BindFactory<LayerData,PlayerController,Star,Star.StarFactory>().FromComponentInNewPrefab(star);
        Container.Bind<Meteor>().FromInstance(meteor).AsTransient();
        
        Container.Bind<ParticleData>().FromInstance(particleData).AsSingle().NonLazy();
        Container.Bind<AudioData>().FromInstance(audioData).AsSingle().NonLazy();
        Container.Bind<FloatData>().FromInstance(floatData).AsSingle().NonLazy();
        Container.Bind<IntData>().FromInstance(intData).AsSingle().NonLazy();
        Container.Bind<LayerData>().FromInstance(layerData).AsSingle().NonLazy();
        Container.Bind<TextData>().FromInstance(textData).AsSingle().NonLazy();
    }
}
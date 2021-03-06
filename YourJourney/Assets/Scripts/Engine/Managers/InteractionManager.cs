﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

/// <summary>
/// Keeps track of all Interactions in the game, creating specific interactions based on their type (Threat,Text etc), randomness, and whether they are token interactions
/// </summary>
public class InteractionManager : MonoBehaviour
{
	public List<IInteraction> interactions { get; set; }
	public List<IInteraction> randomInteractions { get; set; }
	public List<IInteraction> tokenInteractions { get; set; }
	public List<IInteraction> randomTokenInteractions { get; set; }
	public List<IInteraction> allInteractions { get; set; }
	public bool PanelShowing
	{
		get => uiRoot.childCount > 0;
	}

	public GameObject textPanelPrefab, decisionPanelPrefab, statPanelPrefab, spawnMarkerPrefab, damagePanelPrefab;
	public Transform uiRoot;

	[HideInInspector]
	public Engine engine;

	//build list of specific interactions based on each InteractionType in the Scenario
	public void Init( Scenario s )
	{
		engine = FindObjectOfType<Engine>();

		interactions = new List<IInteraction>();

		//first, create a multi-event out of each Trigger that is a multi-trigger
		var multitriggers = engine.scenario.triggersObserver.Where( x => x.isMultiTrigger );
		foreach ( var mt in multitriggers )
		{
			var multi = new MultiEventInteraction();
			multi.GUID = Guid.NewGuid();
			multi.dataName = "converted multievent";
			multi.isEmpty = false;
			multi.triggerName = mt.dataName;
			multi.triggerAfterName = "None";
			//multi.textBookData = new TextBookData();
			//multi.textBookData.pages.Add( "" );
			//multi.eventBookData = new TextBookData();
			//multi.eventBookData.pages.Add( "" );
			multi.isTokenInteraction = false;
			multi.tokenType = TokenType.None;
			multi.eventList = new List<string>();
			multi.triggerList = new List<string>();
			multi.usingTriggers = false;
			multi.isSilent = true;
			multi.interactionType = InteractionType.MultiEvent;

			//add any events listening for this trigger to the eventlist
			Debug.Log( "looking for: " + mt.dataName );
			foreach ( var t in s.interactionObserver )
				Debug.Log( t.triggerName );
			var listeners = s.interactionObserver.Where( x => x.triggerName == mt.dataName );//.Select( y => y.dataName );
			Debug.Log( "found: " + listeners.Count() );
			foreach ( var ev in listeners )
			{
				ev.triggerName = "None";
				multi.eventList.Add( ev.dataName );
			}
			engine.scenario.interactionObserver.Add( multi );
			Debug.Log( "Created MultiEvent placeholder with Events:" + multi.eventList.Count );
		}

		//filter into separate lists

		//ALL interactions
		allInteractions = s.interactionObserver.ToList();
		//NOT random, NOT token
		var ints = s.interactionObserver.Where( x => !x.dataName.Contains( "GRP" ) && !x.isTokenInteraction );
		//IS random, NOT token
		var randomints = s.interactionObserver.Where( x => x.dataName.Contains( "GRP" ) && !x.isTokenInteraction );
		//IS token, random or not
		var toks = s.interactionObserver.Where( x => x.isTokenInteraction );
		//IS random, IS token
		var randomtoks = s.interactionObserver.Where( x => x.dataName.Contains( "GRP" ) && x.isTokenInteraction );

		interactions = ints.Select( x => CreateInteraction( x ) ).ToList();
		randomInteractions = randomints.Select( x => CreateInteraction( x ) ).ToList();
		tokenInteractions = toks.Select( x => CreateInteraction( x ) ).ToList();
		randomTokenInteractions = randomtoks.Select( x => CreateInteraction( x ) ).ToList();

		//Debug.Log( "interactions: " + interactions.Count() );
		//Debug.Log( "randomInteractions: " + randomInteractions.Count() );
		//Debug.Log( "tokenInteractions: " + tokenInteractions.Count() );
		//Debug.Log( "randomTokenInteractions: " + randomTokenInteractions.Count() );
	}

	/// <summary>
	/// Cast to specific Interaction based on the type
	/// </summary>
	IInteraction CreateInteraction( IInteraction interaction )
	{
		if ( interaction.interactionType == InteractionType.Text )
			return (TextInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Threat )
			return (ThreatInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.StatTest )
			return (StatTestInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Decision )
			return (DecisionInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Branch )
			return (BranchInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Darkness )
			return (DarknessInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.MultiEvent )
			return (MultiEventInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Persistent )
			return (PersistentInteraction)interaction;
		else if ( interaction.interactionType == InteractionType.Conditional )
			return (ConditionalInteraction)interaction;

		throw new Exception( "Couldn't create Interaction from: " + interaction.dataName );
	}

	public IInteraction GetInteractionByName( string name )
	{
		if ( allInteractions.Any( x => x.dataName == name ) )
			return allInteractions.Where( x => x.dataName == name ).First();
		else
			throw new Exception( "Couldn't find Interaction: " + name );//this condition should never happen
	}

	public TextPanel GetNewTextPanel()
	{
		return Instantiate( textPanelPrefab, uiRoot ).transform.Find( "TextPanel" ).GetComponent<TextPanel>();
	}

	public DecisionPanel GetNewDecisionPanel()
	{
		return Instantiate( decisionPanelPrefab, uiRoot ).transform.Find( "DecisionPanel" ).GetComponent<DecisionPanel>();
	}

	public StatTestPanel GetNewStatPanel()
	{
		return Instantiate( statPanelPrefab, uiRoot ).transform.Find( "StatTestPanel" ).GetComponent<StatTestPanel>();
	}

	public DamagePanel GetNewDamagePanel()
	{
		return Instantiate( damagePanelPrefab, uiRoot ).transform.Find( "DamagePanel" ).GetComponent<DamagePanel>();
	}

	/// <summary>
	/// Asks if want to use an action on a specified token interaction name.
	/// This is fired when player clicks a token on a tile
	/// </summary>
	public void QueryTokenInteraction( string name, string btnText, Action<InteractionResult> callback )
	{
		//Debug.Log( "QueryTokenInteraction: " + name );
		//remove any MARKERS
		var objs = FindObjectsOfType<SpawnMarker>();
		foreach ( var ob in objs )
		{
			if ( ob.name.Contains( "SPAWNMARKER" ) )
				Destroy( ob.gameObject );
			if ( ob.name == "STARTMARKER" )
				ob.gameObject.SetActive( false );
		}

		if ( tokenInteractions.Count > 0 )
		{
			if ( tokenInteractions.Any( x => x.dataName == name ) )
			{
				IInteraction it = tokenInteractions.Where( x => x.dataName == name ).First();

				//special case for persistent events
				if ( it.interactionType == InteractionType.Persistent && FindObjectOfType<TriggerManager>().IsTriggered( ( (PersistentInteraction)it ).alternativeTextTrigger ) )
				{
					GetNewTextPanel().ShowOkContinue( ( (PersistentInteraction)it ).alternativeBookData.pages[0], ButtonIcon.Continue );
				}
				else
				{
					GetNewTextPanel().ShowQueryInteraction( it, btnText, ( res ) =>
					{
						res.interaction = it;
						res.removeToken = true;

						//DO NOT remove the token if it's a test
						if ( it is StatTestInteraction || it is PersistentInteraction )
							res.removeToken = false;
						callback?.Invoke( res );
					} );
				}
			}
			else
				GetNewTextPanel().ShowOkContinue( $"Data Error (QueryTokenInteraction)\r\nCould not find Interaction with name '{name}'.", ButtonIcon.Continue );
		}
	}

	/// <summary>
	/// Try to fire NON-RANDOM, NON-TOKEN Event based on EVENT NAME
	/// </summary>
	public bool TryFireEventByName( string name )
	{
		Debug.Log( "TryFireEventByName: " + name );
		if ( interactions.Any( x => x.dataName == name ) )
		{
			Debug.Log( "Found Event: " + name );
			ShowInteraction( interactions.Where( x => x.dataName == name ).First() );
			return true;
		}
		else
			Debug.Log( "Couldn't find Event with name: " + name );
		return false;
	}

	/// <summary>
	/// Try to fire NON-RANDOM, NON-TOKEN Event based on TRIGGER NAME
	/// </summary>
	public bool TryFireEventByTrigger( string triggername )
	{
		Debug.Log( "TryFireEventByTrigger: " + triggername );
		if ( interactions.Any( x => x.triggerName == triggername ) )
		{
			int count = interactions.Count( x => x.triggerName == triggername );
			Debug.Log( "Found " + count + " Event(s): " + triggername );
			if ( count == 1 )
			{
				ShowInteraction( interactions.Where( x => x.triggerName == triggername ).First() );
			}
			else
			{
				var inters = interactions.Where( x => x.triggerName == triggername ).ToArray();
				ShowInteraction( inters[UnityEngine.Random.Range( 0, count )] );
			}
			return true;
		}
		else
			Debug.Log( "Couldn't find Event listening to Trigger: " + triggername );
		return false;
	}

	public bool TryFireEndScenario( string name )
	{
		if ( engine.scenario.resolutionObserver.Any( x => x.triggerName == name ) )
		{
			var text = engine.scenario.resolutionObserver.Where( x => x.triggerName == name ).First();
			GetNewTextPanel().ShowOkContinue( text.pages[0], ButtonIcon.Continue );
			return true;
		}
		return false;

	}

	/// <summary>
	/// Show interaction based on type
	/// </summary>
	public void ShowInteraction( IInteraction it, Transform source = null, Action<InteractionResult> action = null )
	{
		if ( it.interactionType == InteractionType.Text )
			HandleText( it );
		else if ( it.interactionType == InteractionType.Threat )
		{
			HandleThreat( it, source ? source.position : ( -1000f ).ToVector3() );
		}
		else if ( it.interactionType == InteractionType.Decision )
		{
			HandleDecision( it );
		}
		else if ( it.interactionType == InteractionType.Branch )
		{
			HandleBranch( it );
		}
		else if ( it.interactionType == InteractionType.StatTest )
		{
			HandleStatTest( it, action );
		}
		else if ( it.interactionType == InteractionType.MultiEvent )
		{
			HandleMultiEvent( it );
		}
		else if ( it.interactionType == InteractionType.Persistent )
		{
			HandlePersistent( it );
		}
		else if ( it.interactionType == InteractionType.Conditional )
		{
			HandleConditional( it );
		}
		else
			GetNewTextPanel().ShowOkContinue( $"Data Error (ShowInteraction)\r\nCould not find Interaction with type '{it.interactionType}'.", ButtonIcon.Continue );
	}

	void HandleText( IInteraction it )
	{
		GetNewTextPanel().ShowTextInteraction( it, () =>
		{
			engine.triggerManager.FireTrigger( it.triggerAfterName );
		} );
	}

	void HandleThreat( IInteraction it, Vector3 position )
	{
		//if it's NOT a token interaction, figure out WHERE to spawn the group
		if ( !it.isTokenInteraction )
		{
			Vector3[] opentf = FindObjectOfType<TileManager>().GetAvailableTokenPositions();

			if ( opentf != null && opentf.Length > 0 )
			{
				int[] rnds = GlowEngine.GenerateRandomNumbers( opentf.Length );
				position = opentf[rnds[0]];
				Debug.Log( "HandleThreat::Using random location" );
			}
			else
			{
				Debug.Log( "HandleThreat::Could not find a random location" );
				return;
			}
		}

		GetNewTextPanel().ShowTextInteraction( it, () =>
		{
			GetNewTextPanel().ShowOkContinue( "Place the enemy figures in the indicated position.", ButtonIcon.Continue );
			engine.triggerManager.FireTrigger( it.triggerAfterName );
		} );
		FindObjectOfType<MonsterManager>().AddNewMonsterGroup( ( (ThreatInteraction)it ).monsterCollection.ToArray(), it );
		if ( position.x != -1000 )
		{
			var go = Instantiate( spawnMarkerPrefab, position, Quaternion.identity );
		}
	}

	void HandleDecision( IInteraction it )
	{
		GetNewDecisionPanel().Show( (DecisionInteraction)it, ( res ) =>
		{
			if ( res.btn1 )
				engine.triggerManager.FireTrigger( ( (DecisionInteraction)it ).choice1Trigger );
			else if ( res.btn2 )
				engine.triggerManager.FireTrigger( ( (DecisionInteraction)it ).choice2Trigger );
			else if ( res.btn3 )
				engine.triggerManager.FireTrigger( ( (DecisionInteraction)it ).choice3Trigger );
			engine.triggerManager.FireTrigger( it.triggerAfterName );
		} );
	}

	void HandleBranch( IInteraction it )
	{
		( (BranchInteraction)it ).Resolve( this );
		engine.triggerManager.FireTrigger( it.triggerAfterName );
	}

	void HandleStatTest( IInteraction it, Action<InteractionResult> action = null )
	{
		GetNewStatPanel().Show( (StatTestInteraction)it, ( b ) =>
		{
			StatTestInteraction sti = (StatTestInteraction)it;

			if ( b.success )//show success textbox
			{
				GetNewTextPanel().ShowOkContinue( sti.passBookData.pages[0], ButtonIcon.Continue/*, () => { engine.triggerManager.FireTrigger( it.triggerAfterName ); }*/ );
				engine.triggerManager.FireTrigger( sti.successTrigger );
				action?.Invoke( new InteractionResult() { removeToken = true } );
			}
			else if ( !b.btn4 && !b.success )//show fail textbox
			{
				GetNewTextPanel().ShowOkContinue( sti.failBookData.pages[0], ButtonIcon.Continue/*, () => { engine.triggerManager.FireTrigger( it.triggerAfterName ); }*/ );
				engine.triggerManager.FireTrigger( sti.failTrigger );
				action?.Invoke( new InteractionResult() { removeToken = true } );
			}
			else if ( b.btn4 )//show progress or success box
			{
				bool success = ( (StatTestInteraction)it ).ResolveCumulative( b.value, engine );
				if ( success )
				{
					GetNewTextPanel().ShowOkContinue( sti.passBookData.pages[0], ButtonIcon.Continue/*, () => { engine.triggerManager.FireTrigger( it.triggerAfterName ); }*/ );
					engine.triggerManager.FireTrigger( sti.successTrigger );
					action?.Invoke( new InteractionResult() { removeToken = true } );
				}
				else
				{
					GetNewTextPanel().ShowOkContinue( sti.progressBookData.pages[0], ButtonIcon.Continue/*, () => { engine.triggerManager.FireTrigger( it.triggerAfterName ); }*/ );
					action?.Invoke( new InteractionResult() { removeToken = false } );
				}
			}
		} );
	}

	void HandleMultiEvent( IInteraction it )
	{
		if ( ( (MultiEventInteraction)it ).isSilent )
		{
			if ( ( (MultiEventInteraction)it ).usingTriggers )
			{
				foreach ( string t in ( (MultiEventInteraction)it ).triggerList )
					engine.triggerManager.FireTrigger( t );
			}
			else
			{
				foreach ( string t in ( (MultiEventInteraction)it ).eventList )
					engine.triggerManager.FireTrigger( t );
			}
			engine.triggerManager.FireTrigger( it.triggerAfterName );
		}
		else
		{
			GetNewTextPanel().ShowTextInteraction( it, () =>
			{
				if ( ( (MultiEventInteraction)it ).usingTriggers )
				{
					foreach ( string t in ( (MultiEventInteraction)it ).triggerList )
						engine.triggerManager.FireTrigger( t );
				}
				else
				{
					foreach ( string t in ( (MultiEventInteraction)it ).eventList )
						engine.triggerManager.FireTrigger( t );
				}
				engine.triggerManager.FireTrigger( it.triggerAfterName );
			} );
		}
	}

	void HandlePersistent( IInteraction it )
	{
		//persistent events are only delegates for activating a "real" event
		//persistent events don't have Event Text or fire "triggerAfterName"
	}

	void HandleConditional( IInteraction it )
	{
		//conditional events don't have a "triggerAfterName"
		//conditional events aren't "triggeredBy" - they only listen
		//conditional events cannot be token interactions - they work silently
	}
}

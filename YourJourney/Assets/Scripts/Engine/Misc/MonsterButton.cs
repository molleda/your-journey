﻿using DG.Tweening;
using UnityEngine;

public class MonsterButton : MonoBehaviour
{
	public Monster monster;
	public CanvasGroup cg;
	public GameObject standard, elite, selected;
	public GameObject[] monsters;//all the monster picture objects

	public bool markRemove = false;

	public void Show( bool isElite )
	{
		standard.SetActive( !isElite );
		elite.SetActive( isElite );

		for ( int i = 0; i < monsters.Length; i++ )
			monsters[i].SetActive( (int)monster.monsterType == i );

		bool hidden = transform.position.x < 130 || transform.position.x > 402;
		if ( !hidden )
		{
			transform.localPosition = new Vector3( transform.localPosition.x, 0, 0 );
			transform.DOLocalMoveY( 25, .5f );
			cg.DOFade( 1, .5f );
		}
	}

	//after a monster is removed on the lineup, animate the remainder into their new positions to fill the gap
	public void Regroup( float newX )
	{
		transform.DOLocalMoveX( newX, .5f );
	}

	public void Remove()
	{
		cg.DOFade( 0, .5f ).OnComplete( () => { Destroy( gameObject ); } );
	}

	public void OnClick()
	{
		var spanel = FindObjectOfType<ShadowPhaseManager>();
		if ( spanel.doingShadowPhase && ( !spanel.allowAttacks || spanel.allowedMonsterGUID != monster.GUID ) )
			return;

		if ( FindObjectOfType<MonsterManager>().ShowCombatPanel( monster ) )
			selected.SetActive( true );
		else
			selected.SetActive( false );
	}

	public void ToggleSelect( bool enabled )
	{
		selected.SetActive( enabled );
	}

	public void Select( string guid )
	{
		if ( guid == monster.GUID.ToString() )
			selected.SetActive( enabled );
	}

	public void ToggleExhausted( bool isExhausted )
	{
		monster.isExhausted = isExhausted;
		if ( !isExhausted )
		{
			monster.isStunned = false;
			cg.alpha = 1;
		}
		else
			cg.alpha = .25f;
	}

	void ToggleVisible( bool visible )
	{
		cg.alpha = visible ? ( monster.isExhausted ? .25f : 1 ) : 0;
		cg.interactable = visible ? true : false;
		cg.blocksRaycasts = visible ? true : false;
	}

	private void Update()
	{
		if ( markRemove )
		{
			cg.blocksRaycasts = false;
			cg.interactable = false;
			return;
		}

		bool hidden = transform.position.x < 130 || transform.position.x > 402;
		ToggleVisible( !hidden );
	}
}

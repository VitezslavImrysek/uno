﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// CCOntrol.cpp, Control_Partial.cpp

#nullable enable

using System;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		private protected override void OnUnloaded()
		{
			RemoveFocusEngagement();

			if (IsFocused && this.GetContext() is Uno.UI.Xaml.Core.CoreServices coreServices)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(this);

				if (focusManager != null)
				{
					// Set the focus on the next focusable element.
					// If we remove the currently focused element from the live tree, inside a GettingFocus or LosingFocus handler,
					// we failfast. This is being tracked by Bug 9840123
					focusManager.SetFocusOnNextFocusableElement(FocusState, true);
				}

				UpdateFocusState(FocusState.Unfocused);
			}

			base.OnUnloaded();
		}

		/// <summary>
		/// Set to the UIElement child which has the IsTemplateFocusTarget attached property set to True, if any.
		/// Otherwise it's set to null.
		/// </summary>
		internal UIElement? FocusTargetDescendant => FindFocusTargetDescendant(this); //TODO Uno: This should be set internally when the template is applied.

		private UIElement? FindFocusTargetDescendant(DependencyObject? root)
		{
			if (root == null)
			{
				return null;
			}

			var children = VisualTreeHelper.GetChildren(root);
			foreach (var child in children)
			{
				if (child == null)
				{
					continue;
				}

				if (child.GetValue(IsTemplateFocusTargetProperty) is bool value && value)
				{
					return child as UIElement;
				}

				// Search recursively
				var innerResult = FindFocusTargetDescendant(child);
				if (innerResult != null)
				{
					return innerResult;
				}
			}

			return null;
		}

		/// <summary>
		/// Sets Focus Engagement on a control, if
		///	    1. The control (or one of its descendants) already has focus
		///     2. Control has IsEngagementEnabled set to true
		/// </summary>
		private void SetFocusEngagement()
		{
			var pFocusManager = VisualTree.GetFocusManagerForElement(this);
			if (pFocusManager != null)
			{
				if (IsFocusEngaged)
				{
					bool hasFocusedElement = FocusProperties.HasFocusedElement(this);
					//Check to see if the element or any of it's descendants has focus
					if (!hasFocusedElement)
					{
						IsFocusEngaged = false;
						throw new InvalidOperationException("Can't engage focus when the control nor any of its descendants has focus.");
					}
					if (!IsFocusEngagementEnabled)
					{
						IsFocusEngaged = false;
						throw new InvalidOperationException("Can't engage focus when IsFocusEngagementEnabled is false on the control.");
					}

					//Control is focused and has IsFocusEngagementEnabled set to true
					pFocusManager.EngagedControl = this;
					UpdateEngagementState(true /*engaging*/);
				}
				else if (pFocusManager.EngagedControl != null) //prevents re-entrancy because we set the property to false above in error cases.
				{
					pFocusManager.EngagedControl = null; /*No control is now engaged*/;
					UpdateEngagementState(false /*Disengage*/);

					var popupRoot = VisualTree.GetPopupRootForElement(this);
					popupRoot?.ClearWasOpenedDuringEngagementOnAllOpenPopups();
				}
			}
		}

		internal void RemoveFocusEngagement()
		{
			if (IsFocusEngaged)
			{
				IsFocusEngaged = false;
			}
		}

		/// <summary>
		/// Raise FocusEngaged and FocusDisengaged events and run
		/// default engagement visuals if necessary.
		/// </summary>
		/// <param name="engage">True if the control is engaging.</param>
		private void UpdateEngagementState(bool engage)
		{
			if (engage)
			{
				var focusEngagedEventArgs = new FocusEngagedEventArgs();
				focusEngagedEventArgs.OriginalSource = this;
				focusEngagedEventArgs.Handled = false;
				FocusEngaged?.Invoke(this, focusEngagedEventArgs);
			}
			else
			{
				var focusDisengagedEventArgs = new FocusDisengagedEventArgs();
				focusDisengagedEventArgs.OriginalSource = this;
				FocusDisengaged?.Invoke(this, focusDisengagedEventArgs);
			}
		}

		internal override bool IsFocusableForFocusEngagement() =>
			IsFocusEngagementEnabled && LastInputGamepad();

		private bool LastInputGamepad()
		{
			var contentRoot = VisualTree.GetContentRootForElement(this);
			return contentRoot?.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote;
		}
	}
}

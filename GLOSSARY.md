# CleanWebApiTemplate

A web API where authenticated callers manage items they own. Every item belongs to exactly one owner, and the
caller's role decides what they may see and do.

Add the terms of each new feature here as they are agreed. Remove the Todo terms when the sample is removed.

## Identity and access

**Caller**:
The authenticated person or system making a request, identified by their email address.
_Avoid_: current user, principal, requester

**Owner**:
The caller who created an item. Ownership is fixed at creation and never transferred; an Admin who creates an item
is its owner.
_Avoid_: creator, author, holder

**Last writer**:
The caller who most recently changed an item. It can differ from the owner when an Admin changes someone else's
item.
_Avoid_: editor, modifier

**Visibility**:
Whether a caller may see and act on an item: an item is visible to its owner and to every Admin, and to no one else.
An item that is not visible is indistinguishable from one that does not exist.
_Avoid_: access, permission, sharing

**User**:
The standard role: a caller who can read, create and change the items they own.
_Avoid_: member, customer, end user

**Operator**:
A User who may also delete the items they own. An Operator sees nothing beyond their own items.
_Avoid_: moderator, power user

**Admin**:
A caller who can see and act on the items of every owner.
_Avoid_: superuser, root, administrator

**External**:
A role reserved for system or third-party integrations. No operation grants it access yet.
_Avoid_: integration user, service account, client

## Todo (sample feature)

**Todo**:
A task owned by one caller, with a title, optional details and a status.
_Avoid_: task, item, ticket

**Title**:
The short name of a Todo. Titles are unique per owner, compared after surrounding whitespace is removed; different
owners may use the same title.
_Avoid_: name, subject

**Status**:
The stage of a Todo: Pending, In progress, Completed or Cancelled. A new Todo starts as Pending.
_Avoid_: state, phase

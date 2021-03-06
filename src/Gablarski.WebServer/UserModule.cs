// Copyright (c) 2010, Eric Maupin
// All rights reserved.
//
// Redistribution and use in source and binary forms, with
// or without modification, are permitted provided that
// the following conditions are met:
//
// - Redistributions of source code must retain the above 
//   copyright notice, this list of conditions and the
//   following disclaimer.
//
// - Redistributions in binary form must reproduce the above
//   copyright notice, this list of conditions and the
//   following disclaimer in the documentation and/or other
//   materials provided with the distribution.
//
// - Neither the name of Gablarski nor the names of its
//   contributors may be used to endorse or promote products
//   or services derived from this software without specific
//   prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS
// AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
// GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
// DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer.Sessions;
using HttpServer;
using Gablarski.Messages;
using Newtonsoft.Json;

namespace Gablarski.WebServer
{
	public class UserModule
		: SectionModule
	{
		public UserModule (ConnectionManager cmanager)
			: base (cmanager, "users")
		{
		}
		
		protected override bool ProcessSection (IHttpRequest request, IHttpResponse response, IHttpSession session)
		{
			if (request.UriParts.Length == 1)
			{
				PermissionDeniedMessage pmsg;
				var listmsg = Connections.SendAndReceive<UserListMessage> (new RequestUserListMessage(UserListMode.All), session, out pmsg);
				
				if (pmsg != null)
				{
					WriteAndFlush (response, "{ \"error\": \"Permission denied\" }");
					return true;
				}
				
				WriteAndFlush (response, JsonConvert.SerializeObject (listmsg.Users.RunQuery (request.QueryString)));
				return true;
			}
			else if (request.UriParts.Length == 2)
			{
				int userId;
				if (!request.TryGetItemId (out userId))
				{
					WriteAndFlush (response, "{ \"error\": \"Invalid request\" }");
					return true;
				}

				switch (request.UriParts[1].Trim().ToLower())
				{
					//case "delete":
					case "edit":
					{
						IHttpInput input = (request.Method.ToLower() == "post") ? request.Form : request.QueryString;

						if (!input.ContainsAndNotNull ("SessionId", "Permissions") || session.Id != input["SessionId"].Value)
						{
							WriteAndFlush (response, "{ \"error\": \"Invalid request\" }");
							return true;
						}

						var permissions = JsonConvert.DeserializeObject<IEnumerable<Permission>> (input["Permissions"].Value).ToList();
						if (permissions.Count == 0)
							return true;

						Connections.Send (new SetPermissionsMessage (userId, permissions), session);
						return true;
					}
				}
			}

			WriteAndFlush (response, "{ \"error\": \"Invalid request\" }");
			return true;
		}
	}
}
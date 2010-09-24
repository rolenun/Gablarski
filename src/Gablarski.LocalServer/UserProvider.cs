﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gablarski.Server;
using NHibernate;
using NHibernate.Linq;

namespace Gablarski.LocalServer
{
	public class UserProvider
		: IUserProvider
	{
		public event EventHandler BansChanged;

		public bool UpdateSupported
		{
			get { return true; }
		}

		public UserRegistrationMode RegistrationMode
		{
			get { return UserRegistrationMode.Normal; }
		}

		public string RegistrationContent
		{
			get { return null; }
		}

		public IEnumerable<IUser> GetUsers()
		{
			using (var session = Persistance.SessionFactory.OpenSession())
				return session.Linq<LocalUser>().Cast<IUser>().ToList();
		}

		public IEnumerable<string> GetAwaitingRegistrations()
		{
			if (RegistrationMode != UserRegistrationMode.Approved)
				throw new NotSupportedException();

			using (var session = Persistance.SessionFactory.OpenSession())
				return session.Linq<AwaitingRegistration>().Select (ar => ar.Username).ToList();
		}

		public void ApproveRegistration (string username)
		{
			if (username == null)
				throw new ArgumentNullException ("username");
			if (RegistrationMode != UserRegistrationMode.Approved)
				throw new NotSupportedException();

			username = username.Trim().ToLower();

			using (var session = Persistance.SessionFactory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var ar = session.Linq<AwaitingRegistration>().SingleOrDefault (r => r.Username.Trim().ToLower() == username);
				if (ar == null)
					return;

				CreateUser (session, ar.Username, ar.HashedPassword);
				session.Delete (ar);

				transaction.Commit();
			}
		}

		public void RejectRegistration (string username)
		{
			if (username == null)
				throw new ArgumentNullException ("username");
			if (RegistrationMode != UserRegistrationMode.Approved)
				throw new NotSupportedException();

			using (var session = Persistance.SessionFactory.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var ar = session.Linq<AwaitingRegistration>().SingleOrDefault (r => r.Username.Trim().ToLower() == username);
				if (ar == null)
					return;

				session.Delete (ar);
				transaction.Commit();
			}
		}

		public bool UserExists (string username)
		{
			if (username == null)
				throw new ArgumentNullException ("username");

			using (var session = Persistance.SessionFactory.OpenSession())
				return UserExists (session, username);
		}

		public IEnumerable<BanInfo> GetBans()
		{
			using (var session = Persistance.SessionFactory.OpenSession())
				return session.Linq<LocalBanInfo>().Cast<BanInfo>().ToList();
		}

		public void AddBan (BanInfo ban)
		{
			if (ban == null)
				throw new ArgumentNullException ("ban");

			using (var session = Persistance.SessionFactory.OpenSession())
			{
				session.SaveOrUpdate (new LocalBanInfo
				{
					Created = DateTime.Now,
					IPMask = ban.IPMask,
					Length = ban.Length,
					Username = ban.Username
				});
			}
		}

		public void RemoveBan (BanInfo ban)
		{
			if (ban == null)
				throw new ArgumentNullException ("ban");

			using (var session = Persistance.SessionFactory.OpenSession())
			{
				LocalBanInfo localBan = ban as LocalBanInfo;
				if (localBan == null)
					localBan = session.Linq<LocalBanInfo>().FirstOrDefault (b => b.Username == ban.Username || b.IPMask == ban.IPMask);

				if (localBan == null)
					return;

				session.Delete (localBan);
			}
		}

		public LoginResult Login (string username, string password)
		{
			if (username == null)
				throw new ArgumentNullException ("username");
			if (password == null)
				throw new ArgumentNullException ("password");

			using (var session = Persistance.SessionFactory.OpenSession())
			{
				var user = session.Linq<LocalUser>().SingleOrDefault (u => u.Username == username);
				if (user == null)
					return new LoginResult (0, LoginResultState.FailedUsername);

				if (user.HashedPassword != HashPassword (password))
					return new LoginResult (0, LoginResultState.FailedPermissions);

				return new LoginResult (user.UserId, LoginResultState.Success);
			}
		}

		public RegisterResult Register (string username, string password)
		{
			if (username == null)
				throw new ArgumentNullException ("username");
			if (password == null)
				throw new ArgumentNullException ("password");

			using (var session = Persistance.SessionFactory.OpenSession())
			{
				if (RegistrationMode == UserRegistrationMode.Normal || RegistrationMode == UserRegistrationMode.PreApproved)
				{
					if (UserExists (session, username))
						return RegisterResult.FailedUsernameInUse;

					CreateUser (session, username, HashPassword (password));

					return RegisterResult.Success;
				}
				else if (RegistrationMode == UserRegistrationMode.Approved)
				{
					session.SaveOrUpdate (new AwaitingRegistration
					{
						Username = username,
						HashedPassword = HashPassword (password)
					});

					return RegisterResult.Success;
				}
				else
					return RegisterResult.FailedUnsupported;
			}
		}

		private bool UserExists (ISession session, string username)
		{
			username = username.Trim().ToLower();

			return session.Linq<LocalUser>().Any (u => u.Username.Trim().ToLower() == username);
		}

		private readonly SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
		private string HashPassword (string password)
		{
			return sha.ComputeHash (Encoding.ASCII.GetBytes (password)).Select (b => b.ToString ("X2")).Aggregate ((s1, s2) => s1 + s2);
		}

		private void CreateUser (ISession session, string username, string hashedPassword)
		{
			session.SaveOrUpdate (new LocalUser
			{
				HashedPassword = hashedPassword,
				Username = username
			});
		}
	}
}